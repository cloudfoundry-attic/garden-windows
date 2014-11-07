package container_test

import (
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"

	"github.com/cloudfoundry-incubator/garden/api"
	netContainer "github.com/pivotal-cf-experimental/garden-dot-net/container"

	"io/ioutil"
	"net/http"
	"strings"

	"github.com/onsi/gomega/ghttp"
)

var _ = Describe("backend", func() {
	var server *ghttp.Server
	var container api.Container
	// var tupperwareURL = flag.String(
	// 	"tupperwareURL",
	// 	"127.0.0.1:80",
	// 	"URL for the Tupperware container server",
	// )

	BeforeEach(func() {
		server = ghttp.NewServer()
		container = netContainer.NewContainer(server.URL())
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
					ghttp.VerifyRequest("PUT", "/api/containers/containerhandle/files"),
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
			err := container.StreamIn("/a/path", strings.NewReader("stuff"))
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
})
