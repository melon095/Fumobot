#!/usr/bin/env -S python3

import requests
import json
import jwt
from datetime import datetime, timedelta
import html

CONFIG_NAME = "/etc/fumobot/config.json"
AUTH_COOKIE_NAME = "seventv-auth"
CSRF_COOKIE_NAME = "seventv-csrf"
LOGIN_ROUTE = "https://7tv.io/v4/auth/login?platform=twitch&return_to=/"

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
    # Initial login button
    pre_stage_one = session.get(LOGIN_ROUTE)
    csrf = pre_stage_one.cookies.get(CSRF_COOKIE_NAME)
    stage_one = pre_stage_one.headers.get("Location")

    # id.twitch.tv
    stage_two_cookies = {}
    for cookie in twitch_cookies.split(";"):
        key, value = cookie.split("=")
        stage_two_cookies[key.strip()] = value.strip()
    
    stage_two = session.get(stage_one, cookies=stage_two_cookies)

    # Extract redirect URL
    stage_three_url = html.unescape(stage_two.text.split("URL='")[1].split("'")[0])

    # Callback
    cookie_resp = session.get(stage_three_url, cookies={CSRF_COOKIE_NAME: csrf})
    auth_cookie = cookie_resp.cookies.get(AUTH_COOKIE_NAME)

    if not auth_cookie:
        raise Exception("Failed to get auth cookie")

    return auth_cookie

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

if __name__ == "__main__":
    main()
