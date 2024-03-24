function service {
    sudo systemctl $1 fumo_bot.service
    sudo systemctl $1 fumo_web.service
}

git pull

service stop

dotnet clean
dotnet build -c Release -o ./Release


service start