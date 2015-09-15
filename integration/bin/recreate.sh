#!/bin/bash

set -ex

pushd "$(dirname "$0")"
go build consume.go && tar -czvf consume.tgz consume.exe &&
  go build -o connect_to_remote_url.exe connect_to_remote_url/main.go && tar -czvf connect_to_remote_url.tgz connect_to_remote_url.exe &&
  go build -o launcher.exe launcher/main.go && tar -czvf launcher.tgz launcher.exe &&
  go build -o loop.exe loop/loop.go && tar -czvf loop.tgz loop.exe
popd
