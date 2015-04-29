package process

import (
	"errors"
	"log"
	"net/url"
	"time"

	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"
	"github.com/onsi/gomega/ghttp"
	"github.com/pivotal-golang/lager/lagertest"

	"github.com/cloudfoundry-incubator/garden"
	"github.com/cloudfoundry-incubator/garden-windows/http_client"
)

var _ = Describe("process", func() {
	var proc DotNetProcess
	var client *http_client.Client

	type IntErrorTuple struct {
		exitStatus int
		err        error
	}

	BeforeEach(func() {
		logger := lagertest.NewTestLogger("process")
		baseUrl, err := url.Parse("http://example.com")
		Expect(err).NotTo(HaveOccurred())
		client = http_client.NewClient(logger, baseUrl)
		proc = NewDotNetProcess("cHandle", client)
	})

	Describe("Id", func() {
		It("returns the pid", func() {
			proc = DotNetProcess{Pid: 9876}
			Expect(proc.ID()).To(Equal(uint32(9876)))
		})
	})

	Describe("Wait", func() {
		var exitStatusChannel chan IntErrorTuple
		BeforeEach(func() {
			exitStatusChannel = make(chan IntErrorTuple, 0)
			go func() {
				exitStatus, err := proc.Wait()
				exitStatusChannel <- IntErrorTuple{exitStatus: exitStatus, err: err}
			}()
		})

		It("waits for the StreamOpen channel to close", func(done Done) {
			close(proc.StreamOpen)

			tuple := <-exitStatusChannel
			Expect(tuple.exitStatus).Should(Equal(0))
			Expect(tuple.err).ShouldNot(HaveOccurred())

			close(done)
		}, 0.1)

		It("returns an erorr if one is sent over StreamOpen channel to close", func(done Done) {
			proc.StreamOpen <- DotNetProcessExitStatus{0, errors.New("An Error Message")}

			tuple := <-exitStatusChannel
			Expect(tuple.err).Should(Equal(errors.New("An Error Message")))
			Expect(tuple.exitStatus).Should(Equal(0))

			close(done)
		}, 0.1)

		It("times out waiting for the StreamOpen channel to close", func() {
			timeout := make(chan bool, 1)
			go func() {
				time.Sleep(100 * time.Millisecond)
				timeout <- true
			}()

			select {
			case <-exitStatusChannel:
				log.Panic("proc.Wait() returned without closing StreamOpen!!!111")
			case <-timeout:
				// we have timed out with the same timeout as the closing test
			}
		})
	})

	Describe("#Signal", func() {
		var server *ghttp.Server

		BeforeEach(func() {
			server = ghttp.NewServer()
			logger := lagertest.NewTestLogger("backend")
			baseUrl, err := url.Parse(server.URL())
			Expect(err).NotTo(HaveOccurred())
			client = http_client.NewClient(logger, baseUrl)

			server.AppendHandlers(
				ghttp.CombineHandlers(
					ghttp.VerifyRequest("DELETE", "/api/containers/cHandle/processes/9876"),
				),
			)
		})

		AfterEach(func() {
			//shut down the server between tests
			if server.HTTPTestServer != nil {
				server.Close()
			}
		})

		It("calls stop on the process through containerizer", func() {
			proc = NewDotNetProcess("cHandle", client)
			proc.Pid = 9876
			err := proc.Signal(garden.SignalKill)
			Expect(err).NotTo(HaveOccurred())
			Expect(server.ReceivedRequests()).Should(HaveLen(1))
		})
	})
})
