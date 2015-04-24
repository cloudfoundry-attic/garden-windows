package backend_test

import (
	"net/url"

	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"

	"github.com/cloudfoundry-incubator/garden"
	"github.com/cloudfoundry-incubator/garden-windows/backend"
	"github.com/cloudfoundry-incubator/garden-windows/container"
	"github.com/cloudfoundry-incubator/garden-windows/containerizer_url"
	"github.com/pivotal-golang/lager/lagertest"

	"time"

	"github.com/onsi/gomega/ghttp"
)

var _ = Describe("backend", func() {
	var server *ghttp.Server
	var dotNetBackend garden.Backend
	var serverUri *url.URL
	var logger *lagertest.TestLogger
	var containerizerURL *containerizer_url.ContainerizerURL

	BeforeEach(func() {
		server = ghttp.NewServer()
		logger = lagertest.NewTestLogger("backend")
		containerizerURL, _ = containerizer_url.FromString(server.URL())
		dotNetBackend, _ = backend.NewDotNetBackend(containerizerURL, logger)
		serverUri, _ = url.Parse(server.URL())
	})

	AfterEach(func() {
		//shut down the server between tests
		if server.HTTPTestServer != nil {
			server.Close()
		}
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
					container.NewContainer(containerizerURL, "MyFirstContainer", logger),
					container.NewContainer(containerizerURL, "MySecondContainer", logger),
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
					container.NewContainer(containerizerURL, "MatchingContainer", logger),
				}))
			})
		})
	})

	Describe("Create", func() {
		var testContainer garden.ContainerSpec

		BeforeEach(func() {
			testContainer = garden.ContainerSpec{
				Handle:     "Fred",
				GraceTime:  1 * time.Second,
				RootFSPath: "/stuff",
				Env: []string{
					"jim",
					"jane",
				},
			}
			server.AppendHandlers(
				ghttp.CombineHandlers(
					ghttp.VerifyRequest("POST", "/api/containers"),
					ghttp.VerifyJSONRepresenting(testContainer),
					ghttp.RespondWith(200, `{"handle":"ServerChangedHandle"}`),
				),
			)
		})

		It("makes a call out to an external service", func() {
			_, err := dotNetBackend.Create(testContainer)
			Expect(err).NotTo(HaveOccurred())
			Expect(server.ReceivedRequests()).Should(HaveLen(1))
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
})
