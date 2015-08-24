package lifecycle

import (
	"github.com/cloudfoundry-incubator/garden"
	"github.com/cloudfoundry-incubator/garden/client/connection"

	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"
)

var _ = Describe("Container information", func() {
	var gardenArgs []string

	BeforeEach(func() {
		gardenArgs = []string{}
		client = startGarden(gardenArgs...)
	})

	Describe("for many containers", func() {
		var handles []string
		BeforeEach(func() {
			container1, err := client.Create(garden.ContainerSpec{})
			Expect(err).ToNot(HaveOccurred())
			container2, err := client.Create(garden.ContainerSpec{})
			Expect(err).ToNot(HaveOccurred())
			handles = []string{container1.Handle(), container2.Handle()}
		})

		AfterEach(func() {
			client.Destroy(handles[0])
			client.Destroy(handles[1])
		})

		Describe(".BulkInfo", func() {
			It("returns container info for the specified handles", func() {
				containers, err := client.Containers(nil)
				Expect(err).ToNot(HaveOccurred())
				Expect(containers).To(HaveLen(2))

				bulkInfo, err := client.BulkInfo(handles)
				Expect(err).ToNot(HaveOccurred())
				Expect(bulkInfo).To(HaveLen(2))
				for _, containerInfoEntry := range bulkInfo {
					Expect(containerInfoEntry.Err).ToNot(HaveOccurred())
				}
			})
		})
	})

	Describe("for a single container", func() {
		var container garden.Container

		BeforeEach(func() {
			var err error

			container, err = client.Create(garden.ContainerSpec{
				Properties: garden.Properties{
					"foo": "bar",
					"a":   "b",
				},
			})
			Expect(err).ToNot(HaveOccurred())
		})

		AfterEach(func() {
			client.Destroy(container.Handle())
		})

		Describe("info for one container", func() {
			It("includes the properties", func() {
				info, err := container.Info()
				Expect(err).ToNot(HaveOccurred())

				Expect(info.Properties["foo"]).To(Equal("bar"))
				Expect(info.Properties["a"]).To(Equal("b"))

				Expect(info.Properties).To(HaveLen(2))
			})
		})

		Describe("getting container properties without getting info", func() {
			It("can list properties", func() {
				err := container.SetProperty("bar", "baz")

				value, err := container.Properties()
				Expect(err).ToNot(HaveOccurred())
				Expect(value).To(HaveKeyWithValue("foo", "bar"))
				Expect(value).To(HaveKeyWithValue("bar", "baz"))
			})
		})

		Describe("updating container properties", func() {
			It("can CRUD", func() {
				value, err := container.Property("foo")
				Expect(err).ToNot(HaveOccurred())
				Expect(value).To(Equal("bar"))

				err = container.SetProperty("foo", "baz")
				Expect(err).ToNot(HaveOccurred())

				err = container.RemoveProperty("a")
				Expect(err).ToNot(HaveOccurred())

				info, err := container.Info()
				Expect(err).ToNot(HaveOccurred())

				Expect(info.Properties).To(Equal(garden.Properties{
					"foo": "baz",
				}))

				err = container.RemoveProperty("foo")
				Expect(err).ToNot(HaveOccurred())

				value, err = container.Property("foo")
				Expect(err).To(HaveOccurred())
				Expect(err.(connection.Error).StatusCode).To(Equal(500))
				Expect(err.(connection.Error).Message).To(Equal("property does not exist: foo"))

				client.Destroy(container.Handle())
				value, err = container.Property("foo")
				Expect(err).To(HaveOccurred())
				Expect(err.(connection.Error).StatusCode).To(Equal(500))
				Expect(err.(connection.Error).Message).To(Equal("unknown handle: " + container.Handle()))
			})
		})

		Describe("listing container info", func() {
			var undesiredHandles []string

			BeforeEach(func() {
				undesiredContainer, err := client.Create(garden.ContainerSpec{
					Properties: garden.Properties{
						"foo": "baz",
						"a":   "b",
					},
				})

				Expect(err).ToNot(HaveOccurred())

				undesiredHandles = append(undesiredHandles, undesiredContainer.Handle())

				undesiredContainer, err = client.Create(garden.ContainerSpec{
					Properties: garden.Properties{
						"baz": "bar",
						"a":   "b",
					},
				})

				Expect(err).ToNot(HaveOccurred())

				undesiredHandles = append(undesiredHandles, undesiredContainer.Handle())
			})

			AfterEach(func() {
				for _, handle := range undesiredHandles {
					err := client.Destroy(handle)
					Expect(err).ToNot(HaveOccurred())
				}
			})

			It("can filter by property", func() {
				containers, err := client.Containers(garden.Properties{"foo": "bar"})
				Expect(err).ToNot(HaveOccurred())

				Expect(containers).To(HaveLen(1))
				Expect(containers[0].Handle()).To(Equal(container.Handle()))

				containers, err = client.Containers(garden.Properties{"matthew": "mcconaughey"})
				Expect(err).ToNot(HaveOccurred())

				Expect(containers).To(BeEmpty())
			})
		})
	})

	Describe("for a non-existent container", func() {
		// NOTE: garden calls List(nil) and find the container with the
		// given handle, (it doesn't call dotNetBackend#Lookup). The only
		// way to call dotNetBackend#Lookup is using a valid handle,
		// e.g. when you call client.Lookup("valid-handle").Info()
		It("returns an error when Lookup is called", func() {
			_, err := client.Lookup("non-existent-handle")
			Expect(err).To(HaveOccurred())
		})
	})
})
