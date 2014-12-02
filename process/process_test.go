package process_test

import (
	"log"
	"time"

	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"

	"github.com/cloudfoundry-incubator/garden/api"
	"github.com/pivotal-cf-experimental/garden-dot-net/process"
)

var _ = Describe("process", func() {
	var proc api.Process

	BeforeEach(func() {
		proc = process.NewDotNetProcess()
	})

	Describe("Wait", func() {
		var exitStatusChannel chan int
		BeforeEach(func() {
			exitStatusChannel = make(chan int, 0)
			go func() {
				exitStatus, err := proc.Wait()
				Ω(err).NotTo(HaveOccurred())
				exitStatusChannel <- exitStatus
			}()
		})

		It("waits for the StreamOpen channel to close", func(done Done) {
			close(proc.(process.DotNetProcess).StreamOpen)

			Ω(<-exitStatusChannel).Should(Equal(0))

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
})
