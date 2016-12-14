package helpers

import (
	"fmt"
	"os"
	"os/exec"
	"path"
	"strconv"
	"strings"
	"syscall"
	"time"

	"code.cloudfoundry.org/garden"
	"github.com/mitchellh/go-ps"
	. "github.com/onsi/gomega"
	"github.com/onsi/gomega/gexec"
	"github.com/pivotal-golang/localip"
	"github.com/tedsuo/ifrit"
	"github.com/tedsuo/ifrit/ginkgomon"
	"github.com/tedsuo/ifrit/grouper"

	garden_runner "code.cloudfoundry.org/garden-windows/integration/runner"
	"github.com/kardianos/osext"
)

func BuildContainerizer() string {
	currentDir, err := osext.ExecutableFolder()
	Expect(err).ShouldNot(HaveOccurred())
	containerizerDir := path.Join(currentDir, "..", "..", "..", "Containerizer", "Containerizer")
	windir := os.Getenv("WINDIR")
	msbuild := path.Join(windir, "Microsoft.NET", "Framework64", "v4.0.30319", "MSBuild")
	cmd := exec.Command(msbuild, `Containerizer.csproj`)
	cmd.Dir = containerizerDir
	Expect(cmd.Run()).To(Succeed(), "Cannot build containerizer. Make sure there are no compilation errors")
	containerizerBin := path.Join(containerizerDir, "bin", "Containerizer.exe")
	Expect(containerizerBin).To(BeAnExistingFile())

	cmd = exec.Command(msbuild, `Containerizer.csproj`)
	cmd.Dir = containerizerDir
	Expect(cmd.Run()).To(Succeed(), "Cannot build containerizer. Make sure there are no compilation errors")
	containerizerBin = path.Join(containerizerDir, "bin", "Containerizer.exe")
	Expect(containerizerBin).To(BeAnExistingFile())
	return containerizerBin
}

func BuildGarden() string {
	gardenPath, err := gexec.Build("code.cloudfoundry.org/garden-windows", "-race")
	Expect(err).ShouldNot(HaveOccurred())
	return gardenPath
}

func StartGarden(gardenBin, containerizerBin string, argv ...string) (ifrit.Process, garden.Client) {
	gardenPort, err := localip.LocalPort()
	Expect(err).NotTo(HaveOccurred())
	gardenAddr := fmt.Sprintf("127.0.0.1:%d", gardenPort)

	tmpDir := os.TempDir()

	// If below fails, try
	// netsh advfirewall firewall add rule name="Open Port 48080"  dir=in action=allow protocol=TCP localport=48080

	containerizerPort, err := localip.LocalPort()
	Expect(err).NotTo(HaveOccurred())
	gardenRunner := garden_runner.New("tcp4", gardenAddr, tmpDir, gardenBin, fmt.Sprintf("http://127.0.0.1:%d", containerizerPort))
	containerizerRunner := ginkgomon.New(ginkgomon.Config{
		Name:              "containerizer",
		Command:           exec.Command(containerizerBin, "--machineIp", "127.0.0.1", "--port", strconv.Itoa(int(containerizerPort))),
		AnsiColorCode:     "",
		StartCheck:        "containerizer.started",
		StartCheckTimeout: 10 * time.Second,
		Cleanup:           func() {},
	})

	group := grouper.NewOrdered(syscall.SIGTERM, []grouper.Member{
		{Name: "containerizer", Runner: containerizerRunner},
		{Name: "garden", Runner: gardenRunner},
	})

	gardenProcess := ifrit.Invoke(group)

	// wait for the processes to start before returning
	<-gardenProcess.Ready()

	return gardenProcess, gardenRunner.NewClient()
}

func StopGarden(process ifrit.Process, client garden.Client) {
	process.Signal(syscall.SIGKILL)
	Eventually(process.Wait(), 10).Should(Receive())
}

func KillAllGarden() {
	killAllProcs("garden-windows")
}

func KillAllContainerizer() {
	killAllProcs("containerizer")
}

func killAllProcs(matchingProcName string) {
	matchingProcName = strings.ToUpper(matchingProcName)

	processes, err := ps.Processes()
	Expect(err).ShouldNot(HaveOccurred())

	for _, proc := range processes {
		procName := strings.ToUpper(proc.Executable())

		if strings.Contains(procName, matchingProcName) {
			parent, err := ps.FindProcess(proc.PPid())
			Expect(err).ShouldNot(HaveOccurred())
			if (parent == nil) || (strings.Contains(parent.Executable(), "test")) {
				goProc, err := os.FindProcess(proc.Pid())
				if err == nil {
					goProc.Kill()
				}
			}
		}
	}
}

func AssertProcessExitsWith(expectedExitCode int, f func() (garden.Process, error)) {
	process, err := f()
	Expect(err).ShouldNot(HaveOccurred())
	actualExitCode, err := process.Wait()
	Expect(err).ShouldNot(HaveOccurred())
	Expect(actualExitCode).To(Equal(expectedExitCode))
}

func AssertEventuallyProcessExitsWith(expectedExitCode int, f func() (garden.Process, error)) {
	Eventually(func() int {
		process, err := f()
		Expect(err).ShouldNot(HaveOccurred())
		actualExitCode, err := process.Wait()
		Expect(err).ShouldNot(HaveOccurred())
		return actualExitCode
	}).Should(Equal(expectedExitCode))
}
