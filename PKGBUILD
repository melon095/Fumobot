pkgname=fumobot-git
pkgrel=1
pkgver=0
arch=(x86_64)
depends=("dotnet-runtime>=9.0" "aspnet-runtime>=9.0" "python-requests" "python-pyjwt")
makedepends=("git" "dotnet-sdk>=9.0" "nodejs" "pnpm")
provides=("${pkgname}")
conflicts=("${pkgname}")
backup=(etc/fumobot/config.json)
source=('git+https://github.com/melon095/fumobot.git')
noextract=()
sha256sums=('SKIP')

_carch="x64"
_framework='net9.0'
_runtime="linux-${_carch}"
_outdir="${pkgver}/${_framework}/${_runtime}"

pkgver() {
	cd "$srcdir/${pkgname%-git}"

	printf "r%s.%s" "$(git rev-list --count HEAD)" "$(git rev-parse --short HEAD)"
}

prepare() {
	cd "$srcdir/${pkgname%-git}"

    pushd src/Fumo.Frontend
        pnpm install --force
    popd

    dotnet restore \
        --runtime "${_runtime}" \
        --locked-mode
}

build() {
	cd "$srcdir/${pkgname%-git}"

    pushd src/Fumo.Frontend
        pnpm run build
    popd

    dotnet build \
        --configuration Release \
        --output "${_outdir}"
}

package() {
	cd "$srcdir/${pkgname%-git}"
    msg "${_outdir}"
    install -dm755 "${pkgdir}/usr/local/bin/fumobot"

    cp -dr "$srcdir/${pkgname%-git}/Scripts/" "${pkgdir}/usr/local/bin/fumobot/Scripts"
    cp -dr "${_outdir}/" "${pkgdir}/usr/local/bin/fumobot"
    cp -dr "$srcdir/${pkgname%-git}/src/Fumo.Frontend/build/" "${pkgdir}/usr/local/bin/fumobot/linux-x64/wwwroot"

    install -Dm644 etc/fumobot.service "${pkgdir}/usr/lib/systemd/system/fumobot.service"
    install -Dm644 etc/fumobot-restart.service "${pkgdir}/usr/lib/systemd/system/fumobot-restart.service"
    install -Dm644 etc/fumobot-seventvtoken.service "${pkgdir}/usr/lib/systemd/system/fumobot-seventvtoken.service"
    install -Dm644 etc/fumobot-seventvtoken.timer "${pkgdir}/usr/lib/systemd/system/fumobot-seventvtoken.timer"
    install -Dm644 etc/fumobot.path "${pkgdir}/usr/lib/systemd/system/fumobot.path"
    install -Dm644 etc/fumobot.sysusers "${pkgdir}/usr/lib/sysusers.d/fumobot.conf"
    install -Dm644 etc/fumobot.tmpfiles "${pkgdir}/usr/lib/tmpfiles.d/fumobot.conf"
}
