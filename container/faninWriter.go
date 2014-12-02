package container

import (
	"errors"
	"io"
	"sync"

	"code.google.com/p/go.net/websocket"
)

type faninWriter struct {
	ws     *websocket.Conn
	closed bool
	writeL sync.Mutex

	hasSink chan struct{}
}

func (w *faninWriter) Write(data []byte) (int, error) {
	<-w.hasSink

	w.writeL.Lock()
	defer w.writeL.Unlock()

	if w.closed {
		return 0, errors.New("write after close")
	}

	websocket.JSON.Send(w.ws, ProcessStreamEvent{
		MessageType: "stdin",
		Data:        string(data),
	})

	return len(data), nil
}

func (w *faninWriter) Close() error {
	<-w.hasSink

	w.writeL.Lock()
	defer w.writeL.Unlock()

	if w.closed {
		return errors.New("closed twice")
	}

	w.closed = true

	return w.ws.Close()
}

func (w *faninWriter) AddSink(sink *websocket.Conn) {
	w.ws = sink
	close(w.hasSink)
}

func (w *faninWriter) AddSource(source io.Reader) {
	go func() {
		_, err := io.Copy(w, source)
		if err == nil {
			w.Close()
		}
	}()
}
