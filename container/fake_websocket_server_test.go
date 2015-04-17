package container_test

import (
	"net"
	"net/http"
	"net/url"
	"sync"

	"github.com/cloudfoundry-incubator/garden-windows/container"
	"github.com/gorilla/mux"
	"github.com/gorilla/websocket"
	"github.com/hydrogen18/stoppableListener"
)

type TestWebSocketServer struct {
	Url       *url.URL
	events    []container.ProcessStreamEvent
	handlerWS *websocket.Conn
	listener  *stoppableListener.StoppableListener
	wg        sync.WaitGroup
	router    *mux.Router
}

var upgrader = websocket.Upgrader{
	ReadBufferSize:  1024,
	WriteBufferSize: 1024,
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
	server.listener, err = stoppableListener.New(originalListener)
	if err != nil {
		panic(err)
	}
	server.Url, err = url.Parse("http://" + originalListener.Addr().String())
	if err != nil {
		panic(err)
	}
}

func (server *TestWebSocketServer) createWebSocketHandler(containerId string) {
	testHandler := func(w http.ResponseWriter, r *http.Request) {
		ws, err := upgrader.Upgrade(w, r, nil)
		if err != nil {
			if _, ok := err.(websocket.HandshakeError); !ok {
				panic(err)
			}
			return
		}
		defer ws.Close()

		server.handlerWS = ws
		for {
			var streamEvent container.ProcessStreamEvent
			err := websocket.ReadJSON(ws, &streamEvent)
			if err != nil {
				continue
			}
			server.events = append(server.events, streamEvent)
			if streamEvent.MessageType == "run" {
				err = websocket.WriteJSON(ws, container.ProcessStreamEvent{
					MessageType: "pid",
					Data:        "5432",
				})
				if err != nil {
					panic(err)
				}
			}
		}
	}

	server.router.HandleFunc("/api/containers/"+containerId+"/run", testHandler)
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
