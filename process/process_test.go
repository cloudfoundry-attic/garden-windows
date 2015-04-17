package process_test

import (
	"errors"
	"log"
	"time"

	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"

	"github.com/cloudfoundry-incubator/garden"
	"github.com/cloudfoundry-incubator/garden-windows/process"
)

var _ = Describe("process", func() {
	var proc garden.Process

	BeforeEach(func() {
		proc = process.NewDotNetProcess()
	})

	type IntErrorTuple struct {
		exitStatus int
		err        error
	}

	Describe("Id", func() {
		It("returns the pid", func() {
			proc = process.DotNetProcess{Id: 9876}
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
			close(proc.(process.DotNetProcess).StreamOpen)

			tuple := <-exitStatusChannel
			Expect(tuple.exitStatus).Should(Equal(0))
			Expect(tuple.err).ShouldNot(HaveOccurred())

			close(done)
		}, 0.1)

		It("returns an erorr if one is sent over StreamOpen channel to close", func(done Done) {
			proc.(process.DotNetProcess).StreamOpen <- process.DotNetProcessExitStatus{0, errors.New("An Error Message")}

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
})
