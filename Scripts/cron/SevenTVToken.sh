#!/usr/bin/env bash

set -x
set -o pipefail

LOGIN_ROUTE="https://7tv.io/v3/auth?platform=twitch"
CONFIG=$(cat config.json)
CURRENT_TOKEN=$(echo $CONFIG | jq -r '.SevenTV.Bearer')
TWITCH_COOKIES=$(echo $CONFIG | jq -r '.Curl.TwitchCookies')
AUTH_COOKIE_NAME="seventv-auth"
CSRF_COOKIE_NAME="seventv-csrf"

check_auth() {
    res=$(curl -s -X POST https://7tv.io/v3/gql -H "Content-Type: application/json" -d '{"query":"{user:actor{id}}"}' -H "Authorization: Bearer $CURRENT_TOKEN" | jq -r '.data.user.id')

    if [ "$res" == "null" ]; then
        return 1
    fi
    
    return 0
}

cookie_helper() {
    local response=$1
    local cookie_name=$2

    echo $(echo "$response" | grep -i "^set-cookie:.*$cookie_name" | awk -F ' ' '{print $2}' | cut -d '=' -f 2 | tr -d ';\r\n')
}

check_auth

if [ $? -eq 0 ]; then
    echo "Token is still valid"
    exit 0
fi

# Initial login button
pre_stage_one=$(curl -s -i -X GET $LOGIN_ROUTE)
csrf=$(cookie_helper "$pre_stage_one" "$CSRF_COOKIE_NAME")
stage_one=$(echo "$pre_stage_one" | grep -i "location" | awk -F ": " '{print $2}' | tr -d '\r\n')

# id.twitch.tv
stage_two=$(curl -s -i -X GET $stage_one --cookie "$TWITCH_COOKIES")

# SevenTV Callback Response
stage_three=$(echo "$stage_two" | grep -o 'URL='\''[^'\'']*'\''' | awk -F"'" '{print $2}' | $(which python3) -c "import sys, html as h; print(h.unescape(sys.stdin.read().strip()))")

cookie_resp=$(curl -s -i -X GET $stage_three --cookie "$CSRF_COOKIE_NAME=$csrf")
auth_cookie=$(cookie_helper "$cookie_resp" "$AUTH_COOKIE_NAME")

if [ -z "$auth_cookie" ]; then
    echo "Failed to get cookie"
    exit 1
fi

NEW_CONFIG=$(echo $CONFIG | jq '.SevenTV.Bearer = "'$auth_cookie'" | .')
echo $CONFIG > config.json.bak
echo $NEW_CONFIG > config.json

check_auth

if [ $? -eq 0 ]; then
    # alias assigned in /etc/aliases 
    mail fumo -s "SevenTV Token Updated" -a "Content-Type: text/html" $EMAIL <<EOF
    <h1>
        SevenTV Token Updated   
    </h1>

    sudo systemctl restart fumo_bot.service
EOF
    exit 0
fi