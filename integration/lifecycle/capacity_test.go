package lifecycle

import (
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"
)

var _ = Describe("Capacity", func() {
	JustBeforeEach(func() {
		client = startGarden(0)
	})

	It("returns positive numbers", func() {
		capacity, err := client.Capacity()
		Expect(err).ToNot(HaveOccurred())
		Expect(capacity.MemoryInBytes).To(BeNumerically(">", 0))
		Expect(capacity.DiskInBytes).To(BeNumerically(">", 0))
		Expect(capacity.MaxContainers).To(BeNumerically(">", 0))
	})
})
