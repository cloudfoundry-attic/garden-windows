package main

import (
	"flag"
	"os"
	"os/signal"
	"syscall"

	"github.com/cloudfoundry-incubator/cf-lager"
	"github.com/cloudfoundry-incubator/garden/server"
	"github.com/pivotal-cf-experimental/garden-dot-net/backend"
	"github.com/pivotal-golang/lager"
)

var listenNetwork = flag.String(
	"listenNetwork",
	"tcp",
	"how to listen on the address (unix, tcp, etc.)",
)

var containerGraceTime = flag.Duration(
	"containerGraceTime",
	0,
	"time after which to destroy idle containers",
)

func main() {
	iface := "0.0.0.0:3333"
	if os.Getenv("PORT") != "" {
		iface = "0.0.0.0:" + os.Getenv("PORT")
	}
	var listenAddr = flag.String(
		"listenAddr",
		iface,
		"address to listen on",
	)

	logger := cf_lager.New("garden-dotnet")

	netBackend := backend.DotNetBackend{}

	gardenServer := server.New(*listenNetwork, *listenAddr, *containerGraceTime, netBackend, logger)
	err := gardenServer.Start()
	if err != nil {
		logger.Fatal("Server Failed to Start", err)
		os.Exit(1)
	}

	logger.Info("started", lager.Data{
		"network": *listenNetwork,
		"addr":    *listenAddr,
	})

	signals := make(chan os.Signal, 1)

	go func() {
		<-signals
		gardenServer.Stop()
		os.Exit(0)
	}()

	signal.Notify(signals, syscall.SIGINT, syscall.SIGTERM, syscall.SIGHUP)
	select {}
}
