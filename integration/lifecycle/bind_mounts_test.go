package lifecycle

import (
	"archive/tar"
	"bytes"
	"fmt"
	"io"
	"io/ioutil"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
	"sync"
	"syscall"

	"code.cloudfoundry.org/garden"
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"
)

var _ = Describe("bind mounts", func() {
	var (
		client      garden.Client
		container   garden.Container
		mount       garden.BindMount
		srcDir      string
		tmpFileName string
		sourceACL   string
		symlinkDir  string
	)

	BeforeEach(func() {
		var err error
		client = startGarden(0)

		srcDir, err = ioutil.TempDir("", "mount.src")
		Expect(err).ShouldNot(HaveOccurred())

		symlinkDir, err = ioutil.TempDir("", "bind-mount-symlink")
		Expect(err).NotTo(HaveOccurred())

		sourceACL = getAcl(srcDir)

		tmpFile, err := ioutil.TempFile(srcDir, "")
		Expect(err).ShouldNot(HaveOccurred())
		tmpFileName = tmpFile.Name()
		tmpFile.Close()

		mount = garden.BindMount{
			SrcPath: srcDir,
			DstPath: "bind-mount-destination",
		}
	})

	AfterEach(func() {
		deleteContainer(container)
		Expect(srcDir).NotTo(Equal(""))
		Expect(os.RemoveAll(srcDir)).To(Succeed())
		Expect(symlinkDir).NotTo(Equal(""))
		Expect(os.RemoveAll(symlinkDir)).To(Succeed())
	})

	It("removes the acls from the bind mount after deleting the container", func() {
		container = createContainerWithMounts(client, []garden.BindMount{mount})
		streamInLsExe(container)

		Expect(getAcl(srcDir)).NotTo(Equal(sourceACL))
		Expect(client.Destroy(container.Handle())).To(Succeed())
		Expect(getAcl(srcDir)).To(Equal(sourceACL))
	})

	Context("when the bind mount source is deleted before the container", func() {
		It("still successfully deletes the container", func() {
			container = createContainerWithMounts(client, []garden.BindMount{mount})
			streamInLsExe(container)

			Expect(os.RemoveAll(srcDir)).To(Succeed())
			Expect(client.Destroy(container.Handle())).To(Succeed())
		})
	})

	It("makes the files visible in the container", func() {
		container = createContainerWithMounts(client, []garden.BindMount{mount})
		streamInLsExe(container)

		stdout := new(bytes.Buffer)
		process, err := container.Run(garden.ProcessSpec{
			Path: "bin/ls-files.exe",
			Args: []string{mount.DstPath},
		}, garden.ProcessIO{Stdout: stdout})
		Expect(err).ShouldNot(HaveOccurred())

		_, err = process.Wait()
		Expect(err).ShouldNot(HaveOccurred())

		Expect(stdout.String()).To(ContainSubstring(filepath.Base(tmpFileName)), stdout.String())
	})

	It("does not allow writing files to the bindmounted directory", func() {
		container = createContainerWithMounts(client, []garden.BindMount{mount})
		streamInLsExe(container)

		stderr := new(bytes.Buffer)

		process, err := container.Run(garden.ProcessSpec{
			Path: "C:\\Windows\\System32\\cmd.exe",
			Args: []string{"/C", "echo hi > " + filepath.Join(mount.DstPath, "out.txt")},
		}, garden.ProcessIO{Stderr: stderr})
		Expect(err).ShouldNot(HaveOccurred())

		exitcode, err := process.Wait()
		Expect(err).ShouldNot(HaveOccurred())

		Expect(filepath.Join(srcDir, "out.txt")).NotTo(BeAnExistingFile())

		Expect(exitcode).NotTo(Equal(0))
		Expect(stderr.String()).To(ContainSubstring("Access is denied"), stderr.String())
	})

	Context("many containers share a bind mount", func() {
		var containers []garden.Container

		BeforeEach(func() {
			sf, err := os.Open(lsExePath)
			defer sf.Close()
			Expect(err).NotTo(HaveOccurred())

			df, err := os.Create(filepath.Join(srcDir, "ls-files.exe"))
			defer df.Close()
			Expect(err).NotTo(HaveOccurred())

			_, err = io.Copy(df, sf)
			Expect(err).NotTo(HaveOccurred())
		})

		AfterEach(func() {
			for _, c := range containers {
				deleteContainer(c)
			}
		})

		It("gives them all the correct access", func() {
			var wg sync.WaitGroup
			for i := 0; i < 20; i++ {
				wg.Add(1)
				go func() {
					defer GinkgoRecover()
					defer wg.Done()
					c := createContainerWithMounts(client, []garden.BindMount{mount})
					containers = append(containers, c)

					stdout := new(bytes.Buffer)
					process, err := c.Run(garden.ProcessSpec{
						Path: filepath.Join(mount.DstPath, "ls-files.exe"),
						Args: []string{mount.DstPath},
					}, garden.ProcessIO{Stdout: stdout})
					Expect(err).ShouldNot(HaveOccurred())

					_, err = process.Wait()
					Expect(err).ShouldNot(HaveOccurred())

					Expect(stdout.String()).To(ContainSubstring(filepath.Base(tmpFileName)), stdout.String())
					Expect(stdout.String()).To(ContainSubstring("ls-files.exe"), stdout.String())
				}()
			}
			wg.Wait()
		})
	})

	Context("the source of the bind mount is a symlink", func() {
		var (
			symlink    string
			symlinkACL string
		)

		BeforeEach(func() {
			symlink = filepath.Join(symlinkDir, "link-dir")
			Expect(createSymlinkToDir(srcDir, symlink)).To(Succeed())

			symlinkACL = getAcl(symlink)

			mount = garden.BindMount{
				SrcPath: symlink,
				DstPath: "bind-mount-destination",
			}
		})

		It("makes the files visible in the container", func() {
			container = createContainerWithMounts(client, []garden.BindMount{mount})
			streamInLsExe(container)

			stdout := new(bytes.Buffer)
			process, err := container.Run(garden.ProcessSpec{
				Path: "bin/ls-files.exe",
				Args: []string{mount.DstPath},
			}, garden.ProcessIO{Stdout: stdout})
			Expect(err).ShouldNot(HaveOccurred())

			_, err = process.Wait()
			Expect(err).ShouldNot(HaveOccurred())

			Expect(stdout.String()).To(ContainSubstring(filepath.Base(tmpFileName)), stdout.String())
		})

		It("removes the acls from the bind mount after deleting the container", func() {
			container = createContainerWithMounts(client, []garden.BindMount{mount})
			streamInLsExe(container)

			Expect(getAcl(symlink)).NotTo(Equal(symlinkACL))
			Expect(client.Destroy(container.Handle())).To(Succeed())
			Expect(getAcl(symlink)).To(Equal(symlinkACL))
			Expect(getAcl(srcDir)).To(Equal(sourceACL))
		})

		It("does not allow writing files to the bindmounted directory", func() {
			container = createContainerWithMounts(client, []garden.BindMount{mount})
			streamInLsExe(container)

			stderr := new(bytes.Buffer)

			process, err := container.Run(garden.ProcessSpec{
				Path: "C:\\Windows\\System32\\cmd.exe",
				Args: []string{"/C", "echo hi > " + filepath.Join(mount.DstPath, "out.txt")},
			}, garden.ProcessIO{Stderr: stderr})
			Expect(err).ShouldNot(HaveOccurred())

			exitcode, err := process.Wait()
			Expect(err).ShouldNot(HaveOccurred())

			Expect(filepath.Join(srcDir, "out.txt")).NotTo(BeAnExistingFile())
			Expect(filepath.Join(symlink, "out.txt")).NotTo(BeAnExistingFile())

			Expect(exitcode).NotTo(Equal(0))
			Expect(stderr.String()).To(ContainSubstring("Access is denied"), stderr.String())
		})

		Context("the source of the bind mount is a symlink to a symlink", func() {
			var symlink2 string

			BeforeEach(func() {
				symlink2 = filepath.Join(symlinkDir, "link-dir-2")
				Expect(createSymlinkToDir(symlink, symlink2)).To(Succeed())

				mount = garden.BindMount{
					SrcPath: symlink2,
					DstPath: "bind-mount-destination",
				}
			})

			It("makes the files visible in the container", func() {
				container = createContainerWithMounts(client, []garden.BindMount{mount})
				streamInLsExe(container)

				stdout := new(bytes.Buffer)
				process, err := container.Run(garden.ProcessSpec{
					Path: "bin/ls-files.exe",
					Args: []string{mount.DstPath},
				}, garden.ProcessIO{Stdout: stdout})
				Expect(err).ShouldNot(HaveOccurred())

				_, err = process.Wait()
				Expect(err).ShouldNot(HaveOccurred())

				Expect(stdout.String()).To(ContainSubstring(filepath.Base(tmpFileName)), stdout.String())
			})

			It("does not allow writing files to the bindmounted directory", func() {
				container = createContainerWithMounts(client, []garden.BindMount{mount})
				streamInLsExe(container)

				stderr := new(bytes.Buffer)

				process, err := container.Run(garden.ProcessSpec{
					Path: "C:\\Windows\\System32\\cmd.exe",
					Args: []string{"/C", "echo hi > " + filepath.Join(mount.DstPath, "out.txt")},
				}, garden.ProcessIO{Stderr: stderr})
				Expect(err).ShouldNot(HaveOccurred())

				exitcode, err := process.Wait()
				Expect(err).ShouldNot(HaveOccurred())

				Expect(filepath.Join(srcDir, "out.txt")).NotTo(BeAnExistingFile())
				Expect(filepath.Join(symlink2, "out.txt")).NotTo(BeAnExistingFile())

				Expect(exitcode).NotTo(Equal(0))
				Expect(stderr.String()).To(ContainSubstring("Access is denied"), stderr.String())
			})
		})
	})

	Context("the source of the bind mount is a unix style path", func() {
		BeforeEach(func() {
			unixStyle := strings.Replace(strings.Replace(srcDir, filepath.VolumeName(srcDir), "", 1), "\\", "/", -1)

			mount = garden.BindMount{
				SrcPath: unixStyle,
				DstPath: "bind-mount-destination",
			}
		})

		It("makes the files visible in the container", func() {
			container = createContainerWithMounts(client, []garden.BindMount{mount})
			streamInLsExe(container)

			stdout := new(bytes.Buffer)
			process, err := container.Run(garden.ProcessSpec{
				Path: "bin/ls-files.exe",
				Args: []string{mount.DstPath},
			}, garden.ProcessIO{Stdout: stdout})
			Expect(err).ShouldNot(HaveOccurred())

			_, err = process.Wait()
			Expect(err).ShouldNot(HaveOccurred())

			Expect(stdout.String()).To(ContainSubstring(filepath.Base(tmpFileName)), stdout.String())
		})

		It("does not allow writing files to the bindmounted directory", func() {
			container = createContainerWithMounts(client, []garden.BindMount{mount})
			streamInLsExe(container)

			stderr := new(bytes.Buffer)

			process, err := container.Run(garden.ProcessSpec{
				Path: "C:\\Windows\\System32\\cmd.exe",
				Args: []string{"/C", "echo hi > " + filepath.Join(mount.DstPath, "out.txt")},
			}, garden.ProcessIO{Stderr: stderr})
			Expect(err).ShouldNot(HaveOccurred())

			exitcode, err := process.Wait()
			Expect(err).ShouldNot(HaveOccurred())

			Expect(filepath.Join(srcDir, "out.txt")).NotTo(BeAnExistingFile())

			Expect(exitcode).NotTo(Equal(0))
			Expect(stderr.String()).To(ContainSubstring("Access is denied"), stderr.String())
		})
	})
})

func createContainerWithMounts(client garden.Client, mounts []garden.BindMount) garden.Container {
	container, err := client.Create(garden.ContainerSpec{
		BindMounts: mounts,
	})
	Expect(err).ShouldNot(HaveOccurred())
	return container
}

func streamInLsExe(container garden.Container) {
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
}

func deleteContainer(c garden.Container) {
	if c == nil {
		return
	}
	_, err := client.Lookup(c.Handle())
	if err == nil {
		Expect(client.Destroy(c.Handle())).To(Succeed())
	} else {
		Expect(err).To(MatchError(garden.ContainerNotFoundError{Handle: c.Handle()}))
	}
}

// used so our chained symlinks are created correctly
// due to go1.9 change, os.Symlink doesn't determine if the symlink is to
// a directory correctly

func createSymlinkToDir(oldname, newname string) error {
	// CreateSymbolicLink is not supported before Windows Vista
	if syscall.LoadCreateSymbolicLink() != nil {
		return &os.LinkError{Op: "symlink", Old: oldname, New: newname, Err: syscall.EWINDOWS}
	}

	n, err := syscall.UTF16PtrFromString(newname)
	if err != nil {
		return &os.LinkError{Op: "symlink", Old: oldname, New: newname, Err: err}
	}
	o, err := syscall.UTF16PtrFromString(oldname)
	if err != nil {
		return &os.LinkError{Op: "symlink", Old: oldname, New: newname, Err: err}
	}

	var flags uint32
	flags |= syscall.SYMBOLIC_LINK_FLAG_DIRECTORY
	err = syscall.CreateSymbolicLink(n, o, flags)
	if err != nil {
		return &os.LinkError{Op: "symlink", Old: oldname, New: newname, Err: err}
	}
	return nil
}

func getAcl(path string) string {
	output, err := exec.Command("powershell", "-command", fmt.Sprintf("(get-acl %s).Access | fl", path)).CombinedOutput()
	Expect(err).NotTo(HaveOccurred())
	return string(output)
}
