#!/bin/sh

SRCDIR=$(realpath "$(dirname "$(realpath "$0")")/../..")

podman image build -f ${SRCDIR}/scripts/ci/linux-arm32v7.docker -t mono-linux-arm32v7 || exit 1

mkdir -p "${SRCDIR}/.podman-home" || exit 1

exec podman run --platform linux/arm/v7 -u root -t --init -a stdin -a stdout -a stderr --volume "${SRCDIR}:/var/hostdir:Z" --volume "${SRCDIR}/.podman-home:/root:Z" -w /var/hostdir --read-only mono-linux-arm32v7 "$@"

