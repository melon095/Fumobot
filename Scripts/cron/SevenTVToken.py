#!/usr/bin/env -S python3

from curl_cffi import requests, Curl, CurlOpt, CurlInfo, CurlError
from io import BytesIO
from requests.structures import CaseInsensitiveDict
import json
import jwt
from datetime import datetime, timedelta
from bs4 import BeautifulSoup
from urllib.parse import urlparse, parse_qs
import traceback

CONFIG_NAME = "/etc/fumobot/config.json"
AUTH_COOKIE_NAME = "seventv-auth"
CSRF_COOKIE_NAME = "seventv-verifier"
LOGIN_ROUTE = b"https://api.7tv.app/v4/auth/login?platform=twitch&return_to=/"
FINALIZE_ROUTE = "https://api.7tv.app/v4/auth/login/finish"

def load_config() -> dict:
    with open(CONFIG_NAME, "r") as f:
        return json.load(f)

def save_config(config) -> None:
    with open(CONFIG_NAME, "w") as f:
        json.dump(config, f, indent=4)

def jwt_not_near_expiration(token: str) -> bool:
    try:
        decoded = jwt.decode(token, options={"verify_signature": False})
        exp = datetime.fromtimestamp(decoded["exp"])
        if exp - datetime.now() <= timedelta(hours=1):
            return False
        return True
    except Exception as e:
        print(f"Error decoding token: {e}")
        return False

def check_auth(session: requests.Session, token: str):
    headers = {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}
    query = {"query": "{user:actor{id}}"}
    response = session.post("https://7tv.io/v3/gql", json=query, headers=headers)
    if response.status_code != 200:
        return False
    data = response.json()
    return data.get("data", {}).get("user", {}).get("id") is not None

def refresh_token(session: requests.Session, twitch_cookies: str) -> str:
    def header_split(buf: BytesIO) -> tuple:
        header_lines = buf.getvalue().decode('iso-8859-1').splitlines()
        headers = CaseInsensitiveDict()
        for line in header_lines:
            if ": " in line:
                key, value = line.split(": ", 1)
                headers[key.strip()] = value.strip()
        return headers

    # Initial login button
    # ja3 thingy :) and requests library is weird
    def stage_one_fn():
        hbuf = BytesIO()
        c = Curl()
        c.setopt(CurlOpt.URL, LOGIN_ROUTE)
        c.setopt(CurlOpt.CUSTOMREQUEST, "GET")
        c.setopt(CurlOpt.HEADERFUNCTION, hbuf.write)
        c.impersonate("chrome124")
        try:
            c.perform()
        except CurlError as e:
            print(f"Error during cURL perform: {e}")
        finally:
            c.close()

        headers = header_split(hbuf)

        csrf = None
        for key, value in headers.items():
            if key.lower() == "set-cookie":
                if value.startswith(CSRF_COOKIE_NAME + "="):
                    csrf = value.split(";")[0].split("=")[1]
                    break
        if not csrf:
            raise Exception("CSRF cookie not found in response headers")
        
        return headers.get("Location"), csrf

    stage_one, csrf = stage_one_fn()
    print(f"CSRF = {csrf}")

    # id.twitch.tv
    stage_two_cookies = {}
    for cookie in twitch_cookies.split(";"):
        key, value = cookie.split("=")
        stage_two_cookies[key.strip()] = value.strip()
    
    stage_two = session.get(stage_one, cookies=stage_two_cookies)

    soup = BeautifulSoup(stage_two.text, "html.parser")
    meta_refresh = soup.find("meta", attrs={"http-equiv": "refresh"})
    if meta_refresh:
        stage_three_url = urlparse(meta_refresh["content"].lower().split("url='")[1].split("'")[0])
        stage_three_code = parse_qs(stage_three_url.query)["code"][0]
    else:
        raise Exception("Failed to find redirect URL")

    # Callback
    def callback_fn(stage_three_url):
        c = Curl()
        c.setopt(CurlOpt.URL, stage_three_url)
        c.setopt(CurlOpt.CUSTOMREQUEST, "GET")
        c.impersonate("chrome124")
        try:
            c.perform()
        except CurlError as e:
            print(f"Error during cURL perform: {e}")
        finally:
            c.close()

    # callback_fn(stage_three_url.geturl())

    def finalize_fn(csrf, stage_three_code):
        jsonbody =json.dumps({
            "code": stage_three_code,
            "platform": "twitch"
        })
        jsonlen = len(jsonbody)
        body = requests.post(FINALIZE_ROUTE, 
                             cookies={CSRF_COOKIE_NAME: csrf},
                             data=jsonbody,
                             headers={
                                "Referer": "https://7tv.app", 
                                "Origin": "https://7tv.app", 
                                "Content-Type": "application/json",
                                "Content-Length": str(jsonlen),
                             },
                             impersonate="chrome124")
        if body.status_code != 200:
            print(f"Finalize request failed with status code: {body.status_code}")
            return None
        
        try:
            data = body.json()
            return data.get("token")
        except json.JSONDecodeError as e:
            print(f"Error parsing finalize response: {e}")
            return None

    auth_token = finalize_fn(csrf, stage_three_code)

    if not auth_token:
        raise Exception("Failed to get auth cookie")

    return auth_token

def main():
    config = load_config()
    token = config.get("SevenTV", {}).get("Bearer")

    session = requests.Session()

    if (token and jwt_not_near_expiration(token)) or check_auth(session, token):
        print("Token is still valid")
        return

    twitch_cookies = config.get("Curl", {}).get("TwitchCookies")
    if not twitch_cookies:
        print("Twitch cookies not found in config")
        return

    try:
        new_token = refresh_token(session, twitch_cookies)
        config["SevenTV"]["Bearer"] = new_token
        save_config(config)

    except Exception as e:
        print(f"Error refreshing token: {e}")
        traceback.print_exc()

if __name__ == "__main__":
    main()
