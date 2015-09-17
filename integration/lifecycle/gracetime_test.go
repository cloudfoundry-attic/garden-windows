package lifecycle

import (
	"time"

	"github.com/cloudfoundry-incubator/garden"

	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"
)

var _ = Describe("Gracetime", func() {
	var gardenArgs []string

	BeforeEach(func() {
		gardenArgs = []string{}
		client = startGarden(gardenArgs...)
	})

	It("can be specified at container creation", func() {
		_, err := client.Create(garden.ContainerSpec{GraceTime: 2 * time.Second})
		Expect(err).NotTo(HaveOccurred())
		containers, err := client.Containers(map[string]string{})
		Expect(err).ShouldNot(HaveOccurred())
		Expect(containers).To(HaveLen(1))

		checkContainers := func() ([]garden.Container, error) {
			return client.Containers(map[string]string{})
		}

		Eventually(checkContainers, 15*time.Second, time.Second).Should(HaveLen(0))
	})
})
