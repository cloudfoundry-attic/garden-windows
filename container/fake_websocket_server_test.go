package container_test

import (
	"net"
	"net/http"
	"net/url"
	"sync"

	"code.google.com/p/go.net/websocket"
	"github.com/gorilla/mux"
	"github.com/hydrogen18/stoppableListener"
	"github.com/pivotal-cf-experimental/garden-dot-net/container"
)

type TestWebSocketServer struct {
	Url       *url.URL
	events    []container.ProcessStreamEvent
	handlerWS *websocket.Conn
	listener  *stoppableListener.StoppableListener
	wg        sync.WaitGroup
	router    *mux.Router
}

func (server *TestWebSocketServer) Start(containerId string) {
	server.router = mux.NewRouter()

	server.createListener()
	server.createWebSocketHandler(containerId)
	server.startWithWaitGroup()
}

func (server *TestWebSocketServer) createListener() {
	originalListener, err := net.Listen("tcp", "localhost:0")
	if err != nil {
		panic(err)
	}
	server.Url, err = url.Parse(originalListener.Addr().String())
	server.listener, err = stoppableListener.New(originalListener)
	if err != nil {
		panic(err)
	}
}

func (server *TestWebSocketServer) createWebSocketHandler(containerId string) {
	testHandler := func(ws *websocket.Conn) {
		server.handlerWS = ws
		for {
			var streamEvent container.ProcessStreamEvent
			websocket.JSON.Receive(ws, &streamEvent)
			server.events = append(server.events, streamEvent)
		}

	}
	server.router.Handle("/api/containers/"+containerId+"/run", websocket.Handler(testHandler))
}

func (server *TestWebSocketServer) startWithWaitGroup() {
	newServer := http.Server{Handler: server.router}
	go func() {
		server.wg.Add(1)
		defer server.wg.Done()
		newServer.Serve(server.listener)
	}()
}

func (server *TestWebSocketServer) Stop() {
	server.listener.Stop()
	server.wg.Wait()
}
