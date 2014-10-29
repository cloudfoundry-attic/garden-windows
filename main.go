package main

import (
	"os"
	"os/signal"
	"syscall"

	"github.com/cloudfoundry-incubator/cf-lager"
	"github.com/cloudfoundry-incubator/garden/server"
	"github.com/pivotal-cf-experimental/garden-dot-net/backend"
	"github.com/pivotal-golang/lager"
)

func main() {
	listenAddr := "0.0.0.0:3000"
	if os.Getenv("PORT") != "" {
		listenAddr = "0.0.0.0:" + os.Getenv("PORT")
	}

	logger := cf_lager.New("garden-dotnet")

	netBackend := backend.DotNetBackend{}

	gardenServer := server.New("tcp", listenAddr, 0, netBackend, logger)
	err := gardenServer.Start()
	if err != nil {
		logger.Fatal("Server Failed to Start", err)
		os.Exit(1)
	}

	logger.Info("started", lager.Data{
		"addr":    listenAddr,
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
