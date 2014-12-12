package container_test

import (
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"

	"github.com/cloudfoundry-incubator/garden/api"
	netContainer "github.com/pivotal-cf-experimental/garden-dot-net/container"
	"github.com/pivotal-cf-experimental/garden-dot-net/process"

	"io/ioutil"

	"errors"
	"strings"

	"code.google.com/p/go.net/websocket"
	"github.com/onsi/gomega/gbytes"
	"github.com/onsi/gomega/ghttp"

	"net/http"
	"net/url"
)

func uint64ptr(n uint64) *uint64 {
	return &n
}

var _ = Describe("container", func() {
	var server *ghttp.Server
	var container api.Container

	BeforeEach(func() {
		server = ghttp.NewServer()
		u, _ := url.Parse(server.URL())
		container = netContainer.NewContainer(*u, "containerhandle")
	})

	AfterEach(func() {
		//shut down the server between tests
		if server.HTTPTestServer != nil {
			server.Close()
		}
	})

	Describe("StreamIn", func() {
		BeforeEach(func() {
			server.AppendHandlers(
				ghttp.CombineHandlers(
					ghttp.VerifyRequest("POST", "/api/containers/containerhandle/files", "destination=a/path"),
					func(w http.ResponseWriter, req *http.Request) {
						body, err := ioutil.ReadAll(req.Body)
						req.Body.Close()
						Ω(err).ShouldNot(HaveOccurred())
						Ω(string(body)).Should(Equal("stuff"))
					},
				),
			)
		})

		It("makes a call out to an external service", func() {
			err := container.StreamIn("a/path", strings.NewReader("stuff"))
			Ω(err).NotTo(HaveOccurred())
			Ω(server.ReceivedRequests()).Should(HaveLen(1))
		})

	})

	Describe("StreamOut", func() {
		BeforeEach(func() {
			server.AppendHandlers(
				ghttp.CombineHandlers(
					ghttp.VerifyRequest("GET", "/api/containers/containerhandle/files", "source=a/path"),
					ghttp.RespondWith(200, "a tarball"),
				),
			)
		})

		It("makes a call out to an external service", func() {
			stream, err := container.StreamOut("a/path")
			Ω(err).NotTo(HaveOccurred())
			Ω(server.ReceivedRequests()).Should(HaveLen(1))

			body, err := ioutil.ReadAll(stream)
			Ω(err).NotTo(HaveOccurred())
			Ω(string(body)).Should(Equal("a tarball"))
		})
	})
	Describe("NetIn", func() {
		It("makes a call out to an external service", func() {
			server.AppendHandlers(
				ghttp.CombineHandlers(
					ghttp.VerifyRequest("POST", "/api/containers/containerhandle/net/in"),
					ghttp.RespondWith(200, `{"hostPort":1234}`),
					func(w http.ResponseWriter, req *http.Request) {
						body, err := ioutil.ReadAll(req.Body)
						req.Body.Close()
						Ω(err).ShouldNot(HaveOccurred())
						Ω(string(body)).Should(Equal(`{"hostPort": 1234}`))
					},
				),
			)
			var hostPort uint32 = 1234
			var containerPort uint32 = 3456
			_, _, err := container.NetIn(hostPort, containerPort)
			Ω(err).NotTo(HaveOccurred())
			Ω(server.ReceivedRequests()).Should(HaveLen(1))
		})

		Context("Containerizer succeeds", func() {
			It("returns containerizers host port", func() {
				server.AppendHandlers(
					ghttp.CombineHandlers(
						ghttp.VerifyRequest("POST", "/api/containers/containerhandle/net/in"),
						ghttp.RespondWith(200, `{"hostPort":9876}`),
					),
				)
				var containerPort uint32 = 3456
				returnedHostPort, returnedContainerPort, err := container.NetIn(1234, containerPort)
				Ω(err).NotTo(HaveOccurred())
				Ω(returnedHostPort).Should(Equal(uint32(9876)))
				Ω(returnedContainerPort).Should(Equal(containerPort))
			})
		})

		Context("Containerizer has an error", func() {
			It("returns the error", func() {
				server.AppendHandlers(
					ghttp.CombineHandlers(
						ghttp.VerifyRequest("POST", "/api/containers/containerhandle/net/in"),
						ghttp.RespondWith(200, `{"error":"Port in use"}`),
					),
				)
				returnedHostPort, returnedContainerPort, err := container.NetIn(1234, 3456)
				Ω(err).Should(MatchError(errors.New("Port in use")))
				Ω(returnedHostPort).Should(Equal(uint32(0)))
				Ω(returnedContainerPort).Should(Equal(uint32(0)))
			})
		})

		Context("Containerizer returns malformed JSON", func() {
			It("returns the error", func() {
				server.AppendHandlers(
					ghttp.CombineHandlers(
						ghttp.VerifyRequest("POST", "/api/containers/containerhandle/net/in"),
						ghttp.RespondWith(200, `hi { fred`),
					),
				)
				returnedHostPort, returnedContainerPort, err := container.NetIn(1234, 3456)
				Ω(err).To(HaveOccurred())
				Ω(returnedHostPort).Should(Equal(uint32(0)))
				Ω(returnedContainerPort).Should(Equal(uint32(0)))
			})
		})
	})

	Describe("#Run (and input / output / error streams)", func() {
		var testServer *TestWebSocketServer

		BeforeEach(func() {
			testServer = &TestWebSocketServer{}
			testServer.Start("containerhandle")
		})

		JustBeforeEach(func() {
			container = netContainer.NewContainer(*testServer.Url, "containerhandle")
		})

		AfterEach(func() {
			testServer.Stop()
		})

		It("runs a script via a websocket and also passes rlimits", func() {
			processSpec := api.ProcessSpec{
				Path: "/some/script",
				Args: []string{"arg1", "arg2"},
				Limits: api.ResourceLimits{
					As:         uint64ptr(1),
					Core:       uint64ptr(2),
					Cpu:        uint64ptr(3),
					Data:       uint64ptr(4),
					Fsize:      uint64ptr(5),
					Locks:      uint64ptr(6),
					Memlock:    uint64ptr(7),
					Msgqueue:   uint64ptr(8),
					Nice:       uint64ptr(9),
					Nofile:     uint64ptr(10),
					Nproc:      uint64ptr(11),
					Rss:        uint64ptr(12),
					Rtprio:     uint64ptr(13),
					Sigpending: uint64ptr(14),
					Stack:      uint64ptr(15),
				},
			}
			_, err := container.Run(processSpec, api.ProcessIO{})

			Ω(err).ShouldNot(HaveOccurred())
			Eventually(func() []netContainer.ProcessStreamEvent {
				return testServer.events
			}).Should(ContainElement(netContainer.ProcessStreamEvent{
				MessageType:    "run",
				ApiProcessSpec: processSpec,
			}))
		})

		It("streams stdout from the websocket back through garden", func() {
			stdout := gbytes.NewBuffer()
			_, err := container.Run(api.ProcessSpec{}, api.ProcessIO{
				Stdout: stdout,
			})
			Ω(err).ShouldNot(HaveOccurred())

			websocket.JSON.Send(testServer.handlerWS, netContainer.ProcessStreamEvent{
				MessageType: "stdout",
				Data:        "hello from windows",
			})
			Eventually(stdout).Should(gbytes.Say("hello from windows"))
		})

		It("streams stderr from the websocket back through garden", func() {
			stderr := gbytes.NewBuffer()
			_, err := container.Run(api.ProcessSpec{}, api.ProcessIO{
				Stderr: stderr,
			})
			Ω(err).ShouldNot(HaveOccurred())

			websocket.JSON.Send(testServer.handlerWS, netContainer.ProcessStreamEvent{
				MessageType: "stderr",
				Data:        "error from windows",
			})
			Eventually(stderr).Should(gbytes.Say("error from windows"))
		})

		It("seperates stdout and stderr streams from the websocket", func() {
			stdout := gbytes.NewBuffer()
			stderr := gbytes.NewBuffer()
			_, err := container.Run(api.ProcessSpec{}, api.ProcessIO{
				Stdout: stdout,
				Stderr: stderr,
			})
			Ω(err).ShouldNot(HaveOccurred())

			websocket.JSON.Send(testServer.handlerWS, netContainer.ProcessStreamEvent{
				MessageType: "stdout",
				Data:        "hello from windows",
			})
			websocket.JSON.Send(testServer.handlerWS, netContainer.ProcessStreamEvent{
				MessageType: "stderr",
				Data:        "error from windows",
			})

			Eventually(stdout).Should(gbytes.Say("hello from windows"))
			Eventually(stderr).Should(gbytes.Say("error from windows"))
		})

		It("streams stdin over the websocket", func() {
			stdin := gbytes.NewBuffer()

			_, err := container.Run(api.ProcessSpec{}, api.ProcessIO{
				Stdin: stdin,
			})
			Ω(err).ShouldNot(HaveOccurred())

			stdin.Write([]byte("a message"))

			Eventually(func() []netContainer.ProcessStreamEvent {
				return testServer.events
			}).Should(ContainElement(netContainer.ProcessStreamEvent{
				MessageType: "stdin",
				Data:        "a message",
			}))

			stdin.Close()
		})

		It("closes the WebSocketOpen channel on the proc when a close event is received", func() {
			proc, err := container.Run(api.ProcessSpec{}, api.ProcessIO{})
			Ω(err).ShouldNot(HaveOccurred())

			websocket.JSON.Send(testServer.handlerWS, netContainer.ProcessStreamEvent{
				MessageType: "close",
			})

			Eventually(proc.(process.DotNetProcess).StreamOpen).Should(BeClosed())
		})

		Context("when we receive an error on the channel", func() {
			It("returns the error", func() {
				proc, err := container.Run(api.ProcessSpec{}, api.ProcessIO{})
				Ω(err).ShouldNot(HaveOccurred())

				websocket.JSON.Send(testServer.handlerWS, netContainer.ProcessStreamEvent{
					MessageType: "error",
					Data:        "An Error Message",
				})

				Eventually(<-proc.(process.DotNetProcess).StreamOpen).Should(Equal("An Error Message"))
			})

			It("closes the WebSocketOpen channel on the proc", func() {
				proc, err := container.Run(api.ProcessSpec{}, api.ProcessIO{})
				Ω(err).ShouldNot(HaveOccurred())

				websocket.JSON.Send(testServer.handlerWS, netContainer.ProcessStreamEvent{
					MessageType: "error",
				})

				Eventually(proc.(process.DotNetProcess).StreamOpen).Should(BeClosed())
			})
		})

		Context("When the containizer server is down", func() {
			BeforeEach(func() {
				testServer.Url = &url.URL{}
			})

			It("returns the error", func(done Done) {
				_, err := container.Run(api.ProcessSpec{}, api.ProcessIO{})
				Ω(err).Should(HaveOccurred())

				close(done)
			}, 1)
		})
	})
})

// It("process.wait returns the exit status", func() {

// Context("with 'privileged' true", func() {
// 	It("runs with --user root", func() {
// 	})
// })

// Context("when spawning fails", func() {
// 	It("returns the error", func() {
// })
