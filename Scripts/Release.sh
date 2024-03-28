function service {
    sudo systemctl $1 fumo_bot.service
}

git pull

service stop

dotnet clean
dotnet build -c Release -o ./Release


service start
