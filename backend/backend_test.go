package backend_test

import (
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"

	"github.com/cloudfoundry-incubator/garden/api"
	"github.com/pivotal-cf-experimental/garden-dot-net/backend"

	"time"

	"github.com/onsi/gomega/ghttp"
)

var _ = Describe("backend", func() {
	var server *ghttp.Server
	var dotNetBackend api.Backend

	BeforeEach(func() {
		server = ghttp.NewServer()
		dotNetBackend = backend.NewDotNetBackend(server.URL())
	})

	AfterEach(func() {
		//shut down the server between tests
		if server.HTTPTestServer != nil {
			server.Close()
		}
	})

	Describe("Create", func() {
		var testContainer api.ContainerSpec

		BeforeEach(func() {
			testContainer = api.ContainerSpec{
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
				),
			)
		})

		It("makes a call out to an external service", func() {
			_, err := dotNetBackend.Create(testContainer)
			Ω(err).NotTo(HaveOccurred())
			Ω(server.ReceivedRequests()).Should(HaveLen(1))
		})

		Context("when there is an error making the http connection", func() {
			It("returns an error", func() {
				server.Close()
				_, err := dotNetBackend.Create(testContainer)
				Ω(err).To(HaveOccurred())
			})
		})
	})

	Describe("Lookup", func() {
		Context("when the handle exists", func() {
			It("returns a container with the correct handle", func() {
				container, err := dotNetBackend.Lookup("someHandle")
				Ω(err).NotTo(HaveOccurred())
				Ω(container.Handle()).To(Equal("someHandle"))
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
			Ω(err).NotTo(HaveOccurred())
			Ω(server.ReceivedRequests()).Should(HaveLen(1))
		})

		Context("when there is an error making the http connection", func() {
			It("returns an error", func() {
				server.Close()
				err := dotNetBackend.Destroy("the world")
				Ω(err).To(HaveOccurred())
			})
		})

	})
})
