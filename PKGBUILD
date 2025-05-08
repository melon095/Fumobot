pkgname=fumobot-git
pkgrel=1
pkgver=0
arch=(x86_64)
depends=("dotnet-runtime>=9.0")
makedepends=("git" "dotnet-sdk>=9.0" "nodejs" "pnpm")
provides=("${pkgname}")
conflicts=("${pkgname}")
replaces=()
backup=()
options=()
install=
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
    install -dm755 "${pkgdir}/usr/lib/fumobot/bin"

    cp -dr "${_outdir}/" "${pkgdir}/usr/lib/fumobot/bin"
    cp -dr "$srcdir/${pkgname%-git}/src/Fumo.Frontend/build/" "${pkgdir}/usr/lib/fumobot/bin/wwwroot"

    install -Dm644 etc/fumobot.service "${pkgdir}/usr/lib/systemd/system/fumobot.service"
    install -Dm644 etc/fumobot-restart.service "${pkgdir}/usr/lib/systemd/system/fumobot-restart.service"
    install -Dm644 etc/fumobot.path "${pkgdir}/usr/lib/systemd/system/fumobot.path"
    install -Dm644 etc/fumobot.sysusers "${pkgdir}/usr/lib/sysusers.d/fumobot.conf"
    install -Dm644 etc/fumobot.tmpfiles "${pkgdir}/usr/lib/tmpfiles.d/fumobot.conf"
}
