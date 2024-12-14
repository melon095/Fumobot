#!/usr/bin/env bash

set -e

if [ "$#" -ne 2 ]; then
    echo "Usage: $0 <SSH_ADDRESS> <TUNNEL_PORT>"
    exit 1
fi

SSH_ADDRESS=$1
TUNNEL_PORT=$2

ssh -vnNT -R $TUNNEL_PORT:localhost:5000 $SSH_ADDRESS
