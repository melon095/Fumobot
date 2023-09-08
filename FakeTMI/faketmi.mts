// #!/usr/bin/env deno
import { WebSocketServer } from "https://deno.land/x/websocket@v0.1.4/mod.ts";

const wss = new WebSocketServer(6969);
wss.on("connection", async function (ws) {
  console.log("Connection received");

  let connected = true;

  ws.on("close", () => {
    connected = false;

    console.log("Connection closed");
  });

  ws.on("message", (message) => {
    if (message.includes("PRIVMSG")) {
      const msg = message.split("PRIVMSG")[1].trim();
      console.log(msg);
    }

    if (message.includes("NICK")) {
      ws.send(
        `:tmi.twitch.tv 001 fumobounce :Welcome, GLHF! tmi.twitch.tv 002 fumobounce :Your host is tmi.twitch.tv :tmi.twitch.tv 003 fumobounce :This server is rather new :tmi.twitch.tv 004 fumobounce :- :tmi.twitch.tv 375 fumobounce :- :tmi.twitch.tv 372 fumobounce :You are in a maze of twisty passages, all alike. :tmi.twitch.tv 376 fumobounce :>`
      );
    }
  });

  while (connected) {
    const buf = new Uint8Array(1024);
    const n = await Deno.stdin.read(buf);
    const input = new TextDecoder().decode(buf.subarray(0, n)).trim();

    if (input) {
      ws.send(CreatePrivmsg(input));
    }
  }
});

function CreatePrivmsg(message) {
  return `@flags=;badge-info=;badges=moderator/1,bits-charity/1;room-id=146910710;id=bb78fca8-7023-4e29-9442-897e3a36d201;user-id=146910710;turbo=0;color=#FF0000;emotes=;display-name=melon095;first-msg=0;mod=1;historical=1;user-type=mod;returning-chatter=0;subscriber=0;rm-received-ts=1693421328783;tmi-sent-ts=1693421328599 :melon095!melon095@melon095.tmi.twitch.tv PRIVMSG #melon095 :${message}\r\n`;
}
