package lifecycle

import (
	"archive/tar"
	"bytes"
	"io"
	"io/ioutil"
	"os"
	"path/filepath"

	"github.com/cloudfoundry-incubator/garden"
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"
	"github.com/onsi/gomega/gexec"
)

var _ = Describe("bind mounts", func() {
	It("works", func() {
		client = startGarden()
		exePath, err := gexec.Build("github.com/cloudfoundry/garden-windows/integration/bin/ls-files", "-race")
		Expect(err).ShouldNot(HaveOccurred())

		tmpDir, err := ioutil.TempDir("", "")
		Expect(err).ShouldNot(HaveOccurred())
		defer func() {
			os.Remove(tmpDir)
		}()

		tmpFile, err := ioutil.TempFile(tmpDir, "")
		Expect(err).ShouldNot(HaveOccurred())
		tempFileName := tmpFile.Name()
		tmpFile.Close()

		mount := garden.BindMount{
			SrcPath: tmpDir,
			DstPath: "bind-mount-destination",
		}
		container, err := client.Create(garden.ContainerSpec{
			BindMounts: []garden.BindMount{mount},
		})
		Expect(err).ShouldNot(HaveOccurred())

		exeFile, err := os.Open(exePath)
		Expect(err).ShouldNot(HaveOccurred())
		defer func() {
			os.Remove(exePath)
		}()
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

		stdout := new(bytes.Buffer)
		process, err := container.Run(garden.ProcessSpec{
			Path: "bin/ls-files.exe",
			Args: []string{mount.DstPath},
		}, garden.ProcessIO{Stdout: stdout})
		Expect(err).ShouldNot(HaveOccurred())

		_, err = process.Wait()
		Expect(err).ShouldNot(HaveOccurred())

		Expect(stdout.String()).To(ContainSubstring(filepath.Base(tempFileName)))
	})
})
