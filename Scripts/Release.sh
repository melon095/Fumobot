function service {
    sudo systemctl $1 fumo_bot.service
}

$OutDir="./Release"

git pull

service stop

dotnet clean -o $OutDir
dotnet build -c Release -o $OutDir

service start
