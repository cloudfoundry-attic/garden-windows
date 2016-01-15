#!/bin/bash

set -ex

pushd "$(dirname "$0")"
go build consume.go && tar -cvf consume.tar consume.exe &&
  go build -o connect_to_remote_url.exe connect_to_remote_url/main.go && tar -cvf connect_to_remote_url.tar connect_to_remote_url.exe &&
  go build -o launcher.exe launcher/main.go && tar -cvf launcher.tar launcher.exe &&
  go build -o loop.exe loop/loop.go && tar -cvf loop.tar loop.exe
popd
