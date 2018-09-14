package lifecycle

import (
	"bufio"
	"bytes"
	"fmt"
	"os"
	"os/exec"
	"strings"

	"code.cloudfoundry.org/garden"
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"
)

var _ = Describe("Websocket connections", func() {
	var c garden.Container
	var err error
	var connsBefore int

	countConns := func() int {
		out, err := exec.Command("netstat", "-an").CombinedOutput()
		Expect(err).NotTo(HaveOccurred())
		scanner := bufio.NewScanner(bytes.NewReader(out))
		scanner.Split(bufio.ScanLines)
		conns := 0
		for scanner.Scan() {
			if strings.Contains(scanner.Text(), fmt.Sprintf(":%d", containerizerPort)) {
				conns++
			}
		}
		return conns
	}

	JustBeforeEach(func() {
		client = startGarden(0)
		c, err = client.Create(garden.ContainerSpec{})
		Expect(err).ToNot(HaveOccurred())
		connsBefore = countConns()
	})

	AfterEach(func() {
		Eventually(func() int {
			return countConns()
		}, "10s", "0.1s").Should(Equal(connsBefore))
		err := client.Destroy(c.Handle())
		Expect(err).ShouldNot(HaveOccurred())
	})

	It("aren't leaked when the process exits without errors", func() {
		googleIPAddress := "74.125.226.164"
		tarFile, err := os.Open("../bin/connect_to_remote_url.tar")
		Expect(err).ShouldNot(HaveOccurred())
		defer tarFile.Close()
		err = c.StreamIn(garden.StreamInSpec{Path: "bin", TarStream: tarFile})
		Expect(err).ShouldNot(HaveOccurred())
		process, err := c.Run(garden.ProcessSpec{
			Path: "bin/connect_to_remote_url.exe",
			Env:  []string{fmt.Sprintf("URL=%v:%v", googleIPAddress, "80")},
		}, garden.ProcessIO{})
		Expect(err).ShouldNot(HaveOccurred())
		_, err = process.Wait()
		Expect(err).ShouldNot(HaveOccurred())
	})
})
