package container_test

import (
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"

	"github.com/cloudfoundry-incubator/garden"
	netContainer "github.com/cloudfoundry-incubator/garden-windows/container"
	"github.com/cloudfoundry-incubator/garden-windows/containerizer_url"
	"github.com/cloudfoundry-incubator/garden-windows/process"

	"io/ioutil"

	"errors"
	"strings"

	"github.com/gorilla/websocket"
	"github.com/onsi/gomega/gbytes"
	"github.com/onsi/gomega/ghttp"
	"github.com/pivotal-golang/lager/lagertest"

	"net/http"
	"net/url"
)

func uint64ptr(n uint64) *uint64 {
	return &n
}

var _ = Describe("container", func() {
	var server *ghttp.Server
	var container garden.Container
	var logger *lagertest.TestLogger
	var containerizerURL *containerizer_url.ContainerizerURL
	var externalIP string

	BeforeEach(func() {
		server = ghttp.NewServer()
		containerizerURL, _ = containerizer_url.FromString(server.URL())
		externalIP = "10.11.12.13"
		logger = lagertest.NewTestLogger("container")
		container = netContainer.NewContainer(containerizerURL, "containerhandle", logger)
	})

	AfterEach(func() {
		//shut down the server between tests
		if server.HTTPTestServer != nil {
			server.Close()
		}
	})

	Describe("Info", func() {
		BeforeEach(func() {
			server.AppendHandlers(
				ghttp.CombineHandlers(
					ghttp.VerifyRequest("GET", "/api/containers/containerhandle/info"),
					ghttp.RespondWith(200, `{"Properties":{"Keymaster": "Gatekeeper"},"MappedPorts":[{"ContainerPort":8080,"HostPort":6543}], "ExternalIP":"`+externalIP+`"}`),
					func(w http.ResponseWriter, req *http.Request) {
						req.Body.Close()
					},
				),
			)
		})

		It("returns info about the container", func() {
			info, err := container.Info()
			Ω(err).NotTo(HaveOccurred())
			Ω(info.Properties).Should(Equal(garden.Properties{"Keymaster": "Gatekeeper"}))

			Ω(info.ExternalIP).Should(Equal(externalIP))

			Ω(len(info.MappedPorts)).Should(Equal(1))
			Ω(info.MappedPorts[0].ContainerPort).Should(Equal(uint32(8080)))
			Ω(info.MappedPorts[0].HostPort).Should(Equal(uint32(6543)))
		})
	})

	Describe("StreamIn", func() {
		Context("Http PUT success request", func() {
			BeforeEach(func() {
				server.AppendHandlers(
					ghttp.CombineHandlers(
						ghttp.VerifyRequest("PUT", "/api/containers/containerhandle/files", "destination=a/path"),
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
		Context("Http PUT failure request", func() {
			BeforeEach(func() {
				server.AppendHandlers(
					ghttp.CombineHandlers(
						ghttp.VerifyRequest("PUT", "/api/containers/containerhandle/files", "destination=a/path"),
						ghttp.RespondWith(500, ``),
						func(w http.ResponseWriter, req *http.Request) {
							req.Body.Close()
						},
					),
				)
			})

			It("makes a call out to an external service", func() {
				err := container.StreamIn("a/path", strings.NewReader("stuff"))
				Ω(err).To(HaveOccurred())
			})
		})

	})

	Describe("StreamOut", func() {
		Context("Containerizer returns 200", func() {
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
				defer stream.Close()
				Ω(err).NotTo(HaveOccurred())
				Ω(server.ReceivedRequests()).Should(HaveLen(1))

				body, err := ioutil.ReadAll(stream)
				Ω(err).NotTo(HaveOccurred())
				Ω(string(body)).Should(Equal("a tarball"))
			})
		})

		Context("Containerizer returns non 200", func() {
			BeforeEach(func() {
				server.AppendHandlers(
					ghttp.CombineHandlers(
						ghttp.VerifyRequest("GET", "/api/containers/containerhandle/files", "source=a/path"),
						ghttp.RespondWith(500, "some large error html text"),
					),
				)
			})

			It("returns an error", func() {
				_, err := container.StreamOut("a/path")
				Ω(err).To(HaveOccurred())
			})
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
			testServerUrl, _ := containerizer_url.FromString(testServer.Url.String())
			container = netContainer.NewContainer(testServerUrl, "containerhandle", logger)
		})

		AfterEach(func() {
			testServer.Stop()
		})

		It("runs a script via a websocket and also passes rlimits", func() {
			processSpec := garden.ProcessSpec{
				Path: "/some/script",
				Args: []string{"arg1", "arg2"},
				Limits: garden.ResourceLimits{
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
			_, err := container.Run(processSpec, garden.ProcessIO{})

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
			_, err := container.Run(garden.ProcessSpec{}, garden.ProcessIO{
				Stdout: stdout,
			})
			Ω(err).ShouldNot(HaveOccurred())

			websocket.WriteJSON(testServer.handlerWS, netContainer.ProcessStreamEvent{
				MessageType: "stdout",
				Data:        "hello from windows",
			})
			Eventually(stdout).Should(gbytes.Say("hello from windows"))
		})

		It("streams stderr from the websocket back through garden", func() {
			stderr := gbytes.NewBuffer()
			_, err := container.Run(garden.ProcessSpec{}, garden.ProcessIO{
				Stderr: stderr,
			})
			Ω(err).ShouldNot(HaveOccurred())

			websocket.WriteJSON(testServer.handlerWS, netContainer.ProcessStreamEvent{
				MessageType: "stderr",
				Data:        "error from windows",
			})
			Eventually(stderr).Should(gbytes.Say("error from windows"))
		})

		It("seperates stdout and stderr streams from the websocket", func() {
			stdout := gbytes.NewBuffer()
			stderr := gbytes.NewBuffer()
			_, err := container.Run(garden.ProcessSpec{}, garden.ProcessIO{
				Stdout: stdout,
				Stderr: stderr,
			})
			Ω(err).ShouldNot(HaveOccurred())

			websocket.WriteJSON(testServer.handlerWS, netContainer.ProcessStreamEvent{
				MessageType: "stdout",
				Data:        "hello from windows",
			})
			websocket.WriteJSON(testServer.handlerWS, netContainer.ProcessStreamEvent{
				MessageType: "stderr",
				Data:        "error from windows",
			})

			Eventually(stdout).Should(gbytes.Say("hello from windows"))
			Eventually(stderr).Should(gbytes.Say("error from windows"))
		})

		It("streams stdin over the websocket", func() {
			stdin := gbytes.NewBuffer()

			_, err := container.Run(garden.ProcessSpec{}, garden.ProcessIO{
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
			proc, err := container.Run(garden.ProcessSpec{}, garden.ProcessIO{})
			Ω(err).ShouldNot(HaveOccurred())

			websocket.WriteJSON(testServer.handlerWS, netContainer.ProcessStreamEvent{
				MessageType: "close",
			})

			Eventually(proc.(process.DotNetProcess).StreamOpen).Should(BeClosed())
		})

		It("returns the close message as the exit code", func() {
			proc, err := container.Run(garden.ProcessSpec{}, garden.ProcessIO{})
			Ω(err).ShouldNot(HaveOccurred())

			websocket.WriteJSON(testServer.handlerWS, netContainer.ProcessStreamEvent{
				MessageType: "close",
				Data:        "27",
			})

			exitCode, err := proc.Wait()

			Expect(exitCode).To(Equal(27))
		})

		Context("when we receive an error on the channel", func() {
			It("returns the error", func(done Done) {
				proc, err := container.Run(garden.ProcessSpec{}, garden.ProcessIO{})
				Ω(err).ShouldNot(HaveOccurred())

				websocket.WriteJSON(testServer.handlerWS, netContainer.ProcessStreamEvent{
					MessageType: "error",
					Data:        "An Error Message",
				})

				exitStatus := <-proc.(process.DotNetProcess).StreamOpen
				Expect(exitStatus.ExitCode).ToNot(Equal(0))
				Expect(exitStatus.Err.Error()).To(Equal("An Error Message"))

				close(done)
			}, 0.2)

			It("closes the WebSocketOpen channel on the proc", func() {
				proc, err := container.Run(garden.ProcessSpec{}, garden.ProcessIO{})
				Ω(err).ShouldNot(HaveOccurred())

				websocket.WriteJSON(testServer.handlerWS, netContainer.ProcessStreamEvent{
					MessageType: "error",
				})

				Eventually(proc.(process.DotNetProcess).StreamOpen).Should(BeClosed())
			})
		})

		Context("When the containizer server is down", func() {
			BeforeEach(func() {
				testServer.Url, _ = url.Parse("http://61CFD780-3ACB-4224-ACBA-C704D8BDD022")
			})

			It("returns the error", func(done Done) {
				_, err := container.Run(garden.ProcessSpec{}, garden.ProcessIO{})
				Ω(err).Should(HaveOccurred())

				close(done)
			}, 1)
		})
	})

	Describe("GetProperties", func() {
		Context("http success", func() {
			BeforeEach(func() {
				server.AppendHandlers(
					ghttp.CombineHandlers(
						ghttp.VerifyRequest("GET", "/api/containers/containerhandle/properties"),
						ghttp.RespondWith(200, `{"Keymaster": "Gatekeeper"}`),
						func(w http.ResponseWriter, req *http.Request) {
							req.Body.Close()
						},
					),
				)
			})

			It("returns info about the container", func() {
				properties, err := container.GetProperties()
				Ω(err).NotTo(HaveOccurred())
				Ω(properties).Should(Equal(garden.Properties{"Keymaster": "Gatekeeper"}))
			})
		})

		Context("http 500 and returns Exception Json", func() {
			BeforeEach(func() {
				server.AppendHandlers(
					ghttp.CombineHandlers(
						ghttp.VerifyRequest("GET", "/api/containers/containerhandle/properties"),
						ghttp.RespondWith(500, `{"Message": "An exception occurred", "ExceptionMessage":"Object reference not set to an instance of an object."}`),
						func(w http.ResponseWriter, req *http.Request) {
							req.Body.Close()
						},
					),
				)
			})
			It("returns ExceptionMessage as error", func() {
				_, err := container.GetProperties()
				Ω(err).To(HaveOccurred())
				Ω(err.Error()).To(Equal("Object reference not set to an instance of an object."))
			})
		})

		Context("http fails and returns non understandable json", func() {
			BeforeEach(func() {
				server.AppendHandlers(
					ghttp.CombineHandlers(
						ghttp.VerifyRequest("GET", "/api/containers/containerhandle/properties"),
						ghttp.RespondWith(500, `{"a":"Some Error Text"}`),
						func(w http.ResponseWriter, req *http.Request) {
							req.Body.Close()
						},
					),
				)
			})
			It("returns http status as error message", func() {
				_, err := container.GetProperties()
				Ω(err).To(HaveOccurred())
				Ω(err.Error()).To(Equal("500 Internal Server Error"))
			})
		})
	})

	Describe("GetProperty", func() {
		BeforeEach(func() {
			server.AppendHandlers(
				ghttp.CombineHandlers(
					ghttp.VerifyRequest("GET", "/api/containers/containerhandle/properties/key:val"),
					ghttp.RespondWith(200, "a value"),
				),
			)
		})

		It("makes a call out to an external service", func() {
			property, err := container.GetProperty("key:val")
			Ω(err).NotTo(HaveOccurred())
			Ω(server.ReceivedRequests()).Should(HaveLen(1))

			Ω(property).Should(Equal("a value"))
		})
	})

	Describe("Stop Container", func() {
		Context("container exists", func() {
			BeforeEach(func() {
				server.AppendHandlers(
					ghttp.CombineHandlers(
						ghttp.VerifyRequest("POST", "/api/containers/containerhandle/stop"),
						ghttp.RespondWith(200, ""),
					),
				)
			})

			It("should stop the container", func() {
				err := container.Stop(true)
				Ω(err).NotTo(HaveOccurred())
				Ω(server.ReceivedRequests()).Should(HaveLen(1))
			})
		})

		Context("container does not exist", func() {
			BeforeEach(func() {
				server.AppendHandlers(
					ghttp.CombineHandlers(
						ghttp.VerifyRequest("POST", "/api/containers/containerhandle/stop"),
						ghttp.RespondWith(404, ""),
					),
				)
			})

			It("should stop the container", func() {
				err := container.Stop(true)
				Ω(err).To(HaveOccurred())
				Ω(err.Error()).To(Equal("404 Not Found"))
			})
		})

		Context("containerizer server is not available", func() {
			BeforeEach(func() {
				server.Close()
			})

			It("should stop the container", func() {
				err := container.Stop(true)
				Ω(err).To(HaveOccurred())
			})
		})
	})

	Describe("SetProperty", func() {
		payloadText := "value"

		BeforeEach(func() {
			server.AppendHandlers(
				ghttp.CombineHandlers(
					ghttp.VerifyRequest("PUT", "/api/containers/containerhandle/properties/key"),
					func() http.HandlerFunc {
						return func(w http.ResponseWriter, req *http.Request) {
							body, err := ioutil.ReadAll(req.Body)
							Ω(err).NotTo(HaveOccurred())
							Ω(string(body)).Should(Equal(payloadText))
						}
					}(),
					ghttp.RespondWith(200, "a body that doesn't matter"),
				),
			)
		})

		It("makes a call out to an external service", func() {
			err := container.SetProperty("key", payloadText)
			Ω(err).NotTo(HaveOccurred())
			Ω(server.ReceivedRequests()).Should(HaveLen(1))
		})
	})

	Describe("RemoveProperty", func() {
		BeforeEach(func() {
			server.AppendHandlers(
				ghttp.CombineHandlers(
					ghttp.VerifyRequest("DELETE", "/api/containers/containerhandle/properties/key"),
					ghttp.RespondWith(204, ""),
				),
			)
		})

		It("makes a call out to an external service", func() {
			err := container.RemoveProperty("key")
			Ω(err).NotTo(HaveOccurred())
			Ω(server.ReceivedRequests()).Should(HaveLen(1))
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
