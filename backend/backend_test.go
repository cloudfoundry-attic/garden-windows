package backend_test

import (
	"errors"
	"fmt"
	"io/ioutil"
	"net"
	"net/http"
	"net/url"

	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"

	"code.cloudfoundry.org/garden"
	"code.cloudfoundry.org/garden-windows/backend"
	"code.cloudfoundry.org/garden-windows/container"
	"code.cloudfoundry.org/garden-windows/dotnet"
	"code.cloudfoundry.org/lager/lagertest"

	"time"

	"github.com/onsi/gomega/ghttp"
)

var _ = Describe("backend", func() {
	var server *ghttp.Server
	var dotNetBackend garden.Backend
	var serverUri *url.URL
	var logger *lagertest.TestLogger
	var client *dotnet.Client

	BeforeEach(func() {
		server = ghttp.NewServer()
		logger = lagertest.NewTestLogger("backend")
		serverUri, _ = url.Parse(server.URL())
		client = dotnet.NewClient(logger, serverUri)
		graceTime := time.Minute
		dotNetBackend, _ = backend.NewDotNetBackend(client, logger, graceTime)
	})

	AfterEach(func() {
		//shut down the server between tests
		if server.HTTPTestServer != nil {
			server.Close()
		}
	})

	Describe("Client", func() {
		Context("when there is an timeout making the http connection", func() {
			BeforeEach(func() {
				client.SetHttpTimeout(100 * time.Millisecond)

				server.AppendHandlers(
					ghttp.CombineHandlers(
						ghttp.VerifyRequest("GET", "/api/capacity"),
						func(http.ResponseWriter, *http.Request) {
							time.Sleep(1 * time.Second)
						},
						ghttp.RespondWith(200, `{"disk_in_bytes":11111,"memory_in_bytes":22222,"max_containers":33333}`),
					),
				)
			})

			It("returns an error", func() {
				_, err := dotNetBackend.Capacity()
				Expect(err).To(HaveOccurred())
				Expect(err.Error()).To(MatchRegexp("use of closed network connection|Client.Timeout exceeded while awaiting headers"))
			})
		})
	})

	Describe("Capacity", func() {
		BeforeEach(func() {
			server.AppendHandlers(
				ghttp.CombineHandlers(
					ghttp.VerifyRequest("GET", "/api/capacity"),
					ghttp.RespondWith(200, `{"disk_in_bytes":11111,"memory_in_bytes":22222,"max_containers":33333}`),
				),
			)
		})

		It("returns capacity", func() {
			capacity, err := dotNetBackend.Capacity()
			Expect(err).NotTo(HaveOccurred())

			Expect(capacity.DiskInBytes).To(Equal(uint64(11111)))
			Expect(capacity.MemoryInBytes).To(Equal(uint64(22222)))
			Expect(capacity.MaxContainers).To(Equal(uint64(33333)))
		})

		Context("when there is an error making the http connection", func() {
			It("returns an error", func() {
				server.Close()
				_, err := dotNetBackend.Capacity()
				Expect(err).To(HaveOccurred())
			})
		})
	})

	Describe("Containers", func() {
		Context("when no properties are specified", func() {
			BeforeEach(func() {
				server.AppendHandlers(
					ghttp.CombineHandlers(
						ghttp.VerifyRequest("GET", "/api/containers"),
						ghttp.RespondWith(200, `["MyFirstContainer","MySecondContainer"]`),
					),
				)
			})
			It("returns a list of containers", func() {
				containers, err := dotNetBackend.Containers(nil)
				Expect(err).NotTo(HaveOccurred())
				Expect(containers).Should(Equal([]garden.Container{
					container.NewContainer(client, "MyFirstContainer", logger),
					container.NewContainer(client, "MySecondContainer", logger),
				}))
			})
		})

		Context("when given properties to filter by", func() {
			BeforeEach(func() {
				server.AppendHandlers(
					ghttp.CombineHandlers(
						ghttp.VerifyRequest("GET", "/api/containers", "q=%7B%22a%22%3A%22c%22%7D"),
						ghttp.RespondWith(200, `["MatchingContainer"]`),
					),
				)
			})
			It("passes them to containerizer and returns only containers with matching properties", func() {
				containers, err := dotNetBackend.Containers(
					garden.Properties{"a": "c"},
				)
				Expect(err).NotTo(HaveOccurred())
				Expect(containers).Should(Equal([]garden.Container{
					container.NewContainer(client, "MatchingContainer", logger),
				}))
			})
		})
	})

	Describe("Create", func() {
		var testContainer garden.ContainerSpec

		Describe("When valid settings", func() {
			ranges := []struct{ start, end string }{
				{"0.0.0.0", "100.100.100.100"},
				{"101.101.101.101", "200.200.200.200"},
				{"201.201.201.201", "255.255.255.255"},
			}
			var rules []garden.NetOutRule
			for _, x := range ranges {
				rules = append(rules, garden.NetOutRule{
					Protocol: garden.ProtocolTCP,
					Networks: []garden.IPRange{{
						Start: net.ParseIP(x.start),
						End:   net.ParseIP(x.end),
					}},
				})
			}

			BeforeEach(func() {
				testContainer = garden.ContainerSpec{
					Handle:     "Fred",
					GraceTime:  1 * time.Second,
					RootFSPath: "/stuff",
					Env: []string{
						"jim",
						"jane",
					},
					NetIn: []garden.NetIn{
						{HostPort: 1234, ContainerPort: 3456},
					},
					NetOut: rules,
				}
				server.AppendHandlers(
					ghttp.CombineHandlers(
						ghttp.VerifyRequest("POST", "/api/containers"),
						ghttp.VerifyJSONRepresenting(testContainer),
						ghttp.RespondWith(200, `{"handle":"ServerChangedHandle"}`),
					),
					ghttp.CombineHandlers(
						ghttp.VerifyRequest("POST", "/api/containers/ServerChangedHandle/net/in"),
						ghttp.RespondWith(200, `{"hostPort":1234}`),
						func(w http.ResponseWriter, req *http.Request) {
							body, err := ioutil.ReadAll(req.Body)
							req.Body.Close()
							Expect(err).ShouldNot(HaveOccurred())
							Expect(string(body)).Should(Equal(`{"hostPort":1234,"containerPort":3456}`))
						},
					),
				)
				for _, x := range ranges {
					expRequest := fmt.Sprintf(`{"protocol":1,"networks":[{"start":"%s","end":"%s"}]}`, x.start, x.end)

					server.AppendHandlers(
						ghttp.CombineHandlers(
							ghttp.VerifyRequest("POST", "/api/containers/ServerChangedHandle/net/out"),
							ghttp.RespondWith(200, ""),
							func(w http.ResponseWriter, req *http.Request) {
								body, err := ioutil.ReadAll(req.Body)
								req.Body.Close()
								Expect(err).ShouldNot(HaveOccurred())
								Expect(string(body)).Should(Equal(expRequest))
							},
						),
					)
				}
			})

			It("makes a call out to an external service", func() {
				_, err := dotNetBackend.Create(testContainer)
				Expect(err).NotTo(HaveOccurred())
				Expect(server.ReceivedRequests()).Should(HaveLen(2 + len(rules)))
			})

			It("sets the container's handle from the response", func() {
				container, err := dotNetBackend.Create(testContainer)
				Expect(err).NotTo(HaveOccurred())
				Expect(container.Handle()).To(Equal("ServerChangedHandle"))
			})

			Context("when there is an error making the http connection", func() {
				It("returns an error", func() {
					server.Close()
					_, err := dotNetBackend.Create(testContainer)
					Expect(err).To(HaveOccurred())
				})
			})
		})

		Describe("When misconfigured NetIn settings", func() {
			BeforeEach(func() {
				testContainer = garden.ContainerSpec{
					Handle:     "Fred",
					GraceTime:  1 * time.Second,
					RootFSPath: "/stuff",
					Env: []string{
						"jim",
						"jane",
					},
					NetIn: []garden.NetIn{
						{HostPort: 1234, ContainerPort: 3456},
					},
				}
				server.AppendHandlers(
					ghttp.CombineHandlers(
						ghttp.VerifyRequest("POST", "/api/containers"),
						ghttp.RespondWith(200, `{"handle":"ServerChangedHandle"}`),
					),
				)
			})
			Context("has an error", func() {
				It("returns the error", func() {
					server.AppendHandlers(
						ghttp.CombineHandlers(
							ghttp.VerifyRequest("POST", "/api/containers/ServerChangedHandle/net/in"),
							ghttp.RespondWith(200, `{"error":"Port in use"}`),
						),
					)
					_, err := dotNetBackend.Create(testContainer)
					Expect(err).Should(MatchError(errors.New("Port in use")))
				})
			})

			Context("returns malformed JSON", func() {
				It("returns the error", func() {
					server.AppendHandlers(
						ghttp.CombineHandlers(
							ghttp.VerifyRequest("POST", "/api/containers/ServerChangedHandle/net/in"),
							ghttp.RespondWith(200, `hi { fred`),
						),
					)
					_, err := dotNetBackend.Create(testContainer)
					Expect(err).To(HaveOccurred())
				})
			})
		})

		Describe("When misconfigured NetOut settings", func() {
			ranges := []struct{ start, end string }{
				{"0.0.0.0", "100.100.100.100"},
				{"101.101.101.101", "200.200.200.200"},
				{"201.201.201.201", "255.255.255.255"},
			}
			var rules []garden.NetOutRule
			for _, x := range ranges {
				rules = append(rules, garden.NetOutRule{
					Protocol: garden.ProtocolTCP,
					Networks: []garden.IPRange{{
						Start: net.ParseIP(x.start),
						End:   net.ParseIP(x.end),
					}},
				})
			}
			BeforeEach(func() {
				testContainer = garden.ContainerSpec{
					Handle:     "Fred",
					GraceTime:  1 * time.Second,
					RootFSPath: "/stuff",
					Env: []string{
						"jim",
						"jane",
					},
					NetOut: rules,
				}
				server.AppendHandlers(
					ghttp.CombineHandlers(
						ghttp.VerifyRequest("POST", "/api/containers"),
						ghttp.RespondWith(200, `{"handle":"ServerChangedHandle"}`),
					),
				)
				for i, x := range ranges {
					expRequest := fmt.Sprintf(`{"protocol":1,"networks":[{"start":"%s","end":"%s"}]}`, x.start, x.end)
					if i >= 1 {
						server.AppendHandlers(
							ghttp.CombineHandlers(
								ghttp.VerifyRequest("POST", "/api/containers/ServerChangedHandle/net/out"),
								ghttp.RespondWith(500, fmt.Sprintf(`"user does not exist: %d"`, i)),
							),
						)
						continue
					}
					server.AppendHandlers(
						ghttp.CombineHandlers(
							ghttp.VerifyRequest("POST", "/api/containers/ServerChangedHandle/net/out"),
							ghttp.RespondWith(200, ""),
							func(w http.ResponseWriter, req *http.Request) {
								body, err := ioutil.ReadAll(req.Body)
								req.Body.Close()
								Expect(err).ShouldNot(HaveOccurred())
								Expect(string(body)).Should(Equal(expRequest))
							},
						),
					)
				}
			})
			It("returns the first error", func() {
				_, err := dotNetBackend.Create(testContainer)
				Expect(err).Should(MatchError(errors.New("user does not exist: 1")))
			})
		})
	})

	Describe("Lookup", func() {
		Context("when the handle exists", func() {
			It("returns a container with the correct handle", func() {
				container, err := dotNetBackend.Lookup("someHandle")
				Expect(err).NotTo(HaveOccurred())
				Expect(container.Handle()).To(Equal("someHandle"))
			})
		})
	})

	Describe("BulkInfo", func() {
		handle1HostIp, handle2HostIp := "10.0.0.20", "10.0.0.21"

		BeforeEach(func() {
			server.AppendHandlers(
				ghttp.CombineHandlers(
					ghttp.VerifyRequest("POST", "/api/bulkcontainerinfo"),
					ghttp.VerifyJSONRepresenting([]string{"handle1", "handle2"}),
					ghttp.RespondWith(200, `{
						"handle1": { "Info": { "HostIP": "`+handle1HostIp+`" } },	
						"handle2": { "Info": { "HostIP": "`+handle2HostIp+`" } }
					}`),
				),
			)
		})

		It("makes a call out to an external service", func() {
			_, err := dotNetBackend.BulkInfo([]string{"handle1", "handle2"})
			Expect(err).NotTo(HaveOccurred())
			Expect(server.ReceivedRequests()).Should(HaveLen(1))
		})

		It("returns the containers info", func() {
			info, err := dotNetBackend.BulkInfo([]string{"handle1", "handle2"})
			Expect(err).NotTo(HaveOccurred())
			Expect(info["handle1"].Info.HostIP).Should(Equal(handle1HostIp))
			Expect(info["handle1"].Err).ShouldNot(HaveOccurred())

			Expect(info["handle2"].Info.HostIP).Should(Equal(handle2HostIp))
			Expect(info["handle2"].Err).ShouldNot(HaveOccurred())
		})

		Context("when there is an error making the http connection", func() {
			It("returns an error", func() {
				server.Close()
				_, err := dotNetBackend.BulkInfo([]string{"hande1", "handle2"})
				Expect(err).To(HaveOccurred())
			})
		})
	})

	Describe("BulkMetrics", func() {
		BeforeEach(func() {
			server.AppendHandlers(
				ghttp.CombineHandlers(
					ghttp.VerifyRequest("POST", "/api/bulkcontainermetrics"),
					ghttp.VerifyJSONRepresenting([]string{"handle1", "handle2"}),
					ghttp.RespondWith(200, `{
						"handle1": { "Metrics": { "MemoryStat": { "TotalUsageTowardLimit": 1234 }}},
						"handle2": { "Metrics": { "MemoryStat": { "TotalUsageTowardLimit": 5678 }}}
					}`),
				),
			)
		})

		It("makes a call out to an external service", func() {
			_, err := dotNetBackend.BulkMetrics([]string{"handle1", "handle2"})
			Expect(err).NotTo(HaveOccurred())
			Expect(server.ReceivedRequests()).Should(HaveLen(1))
		})

		It("returns the containers metrics", func() {
			metrics, err := dotNetBackend.BulkMetrics([]string{"handle1", "handle2"})
			Expect(err).NotTo(HaveOccurred())
			Expect(metrics["handle1"].Metrics.MemoryStat.TotalUsageTowardLimit).Should(Equal(uint64(1234)))
			Expect(metrics["handle1"].Err).ShouldNot(HaveOccurred())

			Expect(metrics["handle2"].Metrics.MemoryStat.TotalUsageTowardLimit).Should(Equal(uint64(5678)))
			Expect(metrics["handle2"].Err).ShouldNot(HaveOccurred())
		})

		Context("when there is an error making the http connection", func() {
			It("returns an error", func() {
				server.Close()
				_, err := dotNetBackend.BulkMetrics([]string{"hande1", "handle2"})
				Expect(err).To(HaveOccurred())
			})
		})
	})

	Describe("Destroy", func() {
		BeforeEach(func() {
			server.AppendHandlers(
				ghttp.CombineHandlers(
					ghttp.VerifyRequest("DELETE", "/api/containers/bob"),
				),
			)
		})

		It("makes a call out to an external service", func() {
			err := dotNetBackend.Destroy("bob")
			Expect(err).NotTo(HaveOccurred())
			Expect(server.ReceivedRequests()).Should(HaveLen(1))
		})

		Context("when there is an error making the http connection", func() {
			It("returns an error", func() {
				server.Close()
				err := dotNetBackend.Destroy("the world")
				Expect(err).To(HaveOccurred())
			})
		})

	})

	Describe("Ping", func() {
		BeforeEach(func() {
			server.AppendHandlers(
				ghttp.CombineHandlers(
					ghttp.VerifyRequest("GET", "/api/ping"),
				),
			)
		})

		Context("windows containerizer server is up", func() {
			It("makes a call out to an external service", func() {
				err := dotNetBackend.Ping()
				Expect(err).NotTo(HaveOccurred())
				Expect(server.ReceivedRequests()).Should(HaveLen(1))
			})
		})

		Context("windows containerizer server is down", func() {
			BeforeEach(func() {
				server.Close()
			})

			It("returns an error", func() {
				err := dotNetBackend.Ping()
				Expect(err).To(HaveOccurred())
			})
		})
	})

	Describe("GraceTime", func() {
		var testContainer garden.ContainerSpec

		Context("containerizer GraceTime endpoint succeeds", func() {
			BeforeEach(func() {
				testContainer = garden.ContainerSpec{
					GraceTime: 2 * time.Second,
				}
				server.AppendHandlers(
					ghttp.CombineHandlers(
						ghttp.VerifyRequest("POST", "/api/containers"),
						ghttp.VerifyJSONRepresenting(testContainer),
						ghttp.RespondWith(200, `{"handle":"handle1"}`),
					),
					ghttp.CombineHandlers(
						ghttp.VerifyRequest("GET", "/api/containers/handle1/grace_time"),
						ghttp.RespondWith(200, `{"grace_time":2000000000}`),
					),
				)
			})

			It("returns the container gracetime", func() {
				container, err := dotNetBackend.Create(testContainer)
				Expect(err).NotTo(HaveOccurred())
				Expect(container.Handle()).To(Equal("handle1"))
				graceTime := dotNetBackend.GraceTime(container)
				Expect(graceTime).Should(Equal(2 * time.Second))
			})
		})

		Context("containerizer GraceTime endpoint fails", func() {
			BeforeEach(func() {
				testContainer = garden.ContainerSpec{
					GraceTime: 2 * time.Second,
				}
				server.AppendHandlers(
					ghttp.CombineHandlers(
						ghttp.VerifyRequest("POST", "/api/containers"),
						ghttp.VerifyJSONRepresenting(testContainer),
						ghttp.RespondWith(200, `{"handle":"handle1"}`),
					),
					ghttp.CombineHandlers(
						ghttp.VerifyRequest("GET", "/api/containers/handle1/grace_time"),
						ghttp.RespondWith(500, `catastrophic error`),
					),
				)
			})

			It("returns the default backend gracetime", func() {
				container, err := dotNetBackend.Create(testContainer)
				Expect(err).NotTo(HaveOccurred())
				Expect(container.Handle()).To(Equal("handle1"))

				graceTime := dotNetBackend.GraceTime(container)
				Expect(graceTime).Should(Equal(1 * time.Minute))
			})
		})
	})
})
