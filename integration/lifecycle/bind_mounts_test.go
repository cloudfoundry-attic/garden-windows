package lifecycle

import (
	"archive/tar"
	"bytes"
	"io"
	"io/ioutil"
	"os"
	"path/filepath"

	"code.cloudfoundry.org/garden"
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"
)

var _ = Describe("bind mounts", func() {
	var (
		client      garden.Client
		container   garden.Container
		mount       garden.BindMount
		tmpDir      string
		tmpFileName string
	)

	BeforeEach(func() {
		var err error
		client = startGarden(0)

		tmpDir, err = ioutil.TempDir("", "")
		Expect(err).ShouldNot(HaveOccurred())

		tmpFile, err := ioutil.TempFile(tmpDir, "")
		Expect(err).ShouldNot(HaveOccurred())
		tmpFileName = tmpFile.Name()
		tmpFile.Close()

		mount = garden.BindMount{
			SrcPath: tmpDir,
			DstPath: "bind-mount-destination",
		}
		container, err = client.Create(garden.ContainerSpec{
			BindMounts: []garden.BindMount{mount},
		})
		Expect(err).ShouldNot(HaveOccurred())

		exeFile, err := os.Open(lsExePath)
		Expect(err).ShouldNot(HaveOccurred())
		defer exeFile.Close()

		fi, err := exeFile.Stat()
		Expect(err).ShouldNot(HaveOccurred())
		head, err := tar.FileInfoHeader(fi, "")
		Expect(err).ShouldNot(HaveOccurred())
		buf := new(bytes.Buffer)
		wr := tar.NewWriter(buf)
		err = wr.WriteHeader(head)
		Expect(err).ShouldNot(HaveOccurred())

		_, err = io.Copy(wr, exeFile)
		Expect(err).ShouldNot(HaveOccurred())
		err = wr.Close()
		Expect(err).ShouldNot(HaveOccurred())
		err = container.StreamIn(garden.StreamInSpec{
			TarStream: buf,
			Path:      "bin",
		})
		Expect(err).ShouldNot(HaveOccurred())
	})

	AfterEach(func() {
		Expect(client.Destroy(container.Handle())).To(Succeed())
		Expect(os.RemoveAll(tmpDir)).To(Succeed())
	})

	It("makes the files visible in the container", func() {
		stdout := new(bytes.Buffer)
		process, err := container.Run(garden.ProcessSpec{
			Path: "bin/ls-files.exe",
			Args: []string{mount.DstPath},
		}, garden.ProcessIO{Stdout: stdout})
		Expect(err).ShouldNot(HaveOccurred())

		_, err = process.Wait()
		Expect(err).ShouldNot(HaveOccurred())

		Expect(stdout.String()).To(ContainSubstring(filepath.Base(tmpFileName)))
	})

	It("does not allow writing files to the bindmounted directory", func() {
		stderr := new(bytes.Buffer)

		process, err := container.Run(garden.ProcessSpec{
			Path: "C:\\Windows\\System32\\cmd.exe",
			Args: []string{"/C", "echo hi > " + filepath.Join(mount.DstPath, "out.txt")},
		}, garden.ProcessIO{Stderr: stderr})
		Expect(err).ShouldNot(HaveOccurred())

		exitcode, err := process.Wait()
		Expect(err).ShouldNot(HaveOccurred())

		Expect(filepath.Join(tmpDir, "out.txt")).NotTo(BeAnExistingFile())

		Expect(exitcode).NotTo(Equal(0))
		Expect(stderr.String()).To(ContainSubstring("Access is denied"))
	})
})
