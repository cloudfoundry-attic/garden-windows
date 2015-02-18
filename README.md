# Garden Windows 
(Windows backend for Garden)

For more information on Diego and Garden, pleasd refer to: [Garden](https://github.com/cloudfoundry-incubator/garden).

## To run on *nix


    git clone https://github.com/pivotal-cf-experimental/diego-release.git diego-release

    cd diego-release

    git checkout working

    source .envrc

    scripts/update

    cd $GOPATH/src/github.com/cloudfoundry-incubator/garden-windows/

    go install

    cd $GOPATH/bin

    ./garden-windows -listenNetwork=unix -listenAddr=/tmp/garden-windows.sock -containerGraceTime=1h -containerizerURL=http://52.0.209.104:80/


NB: the ip address should point to a running containerizer.

If it worked correctly, you will see output like this:

    {"timestamp":"1424276697.329845428","source":"garden-windows","message":"garden-windows.started","log_level":1,"data":{"addr":"/tmp/garden-windows.sock","network":"unix"}}

If it does not connect to containerizer, you will see no output at all. Make sure containerizer is running, and that the ontainerizerURL address is correct.


