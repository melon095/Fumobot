function service {
    sudo systemctl $1 fumo_bot.service
}

OutDir="Release"

git pull

service stop

dotnet clean -o $OutDir

if [ "$1" == "frontend" ]; then
    dotnet build -c Release -o $OutDir -p:BuildFrontend=true
else
    dotnet build -c Release -o $OutDir
fi

service start
