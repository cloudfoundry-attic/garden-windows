package helpers

import (
	"fmt"
	"os"
	"os/exec"
	"path"
	"strconv"
	"syscall"
	"time"

	"github.com/cloudfoundry-incubator/garden"
	. "github.com/onsi/gomega"
	"github.com/onsi/gomega/gexec"
	"github.com/tedsuo/ifrit"
	"github.com/tedsuo/ifrit/ginkgomon"
	"github.com/tedsuo/ifrit/grouper"

	garden_runner "github.com/cloudfoundry-incubator/garden-windows/integration/runner"
	"github.com/cloudfoundry/loggregator/src/bitbucket.org/kardianos/osext"
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
	gardenPath, err := gexec.Build("github.com/cloudfoundry-incubator/garden-windows", "-a", "-race", "-tags", "daemon")
	Expect(err).ShouldNot(HaveOccurred())
	return gardenPath
}

func StartGarden(gardenBin, containerizerBin string, argv ...string) (ifrit.Process, garden.Client) {
	gardenAddr := fmt.Sprintf("127.0.0.1:45607")

	tmpDir := os.TempDir()

	// If below fails, try
	// netsh advfirewall firewall add rule name="Open Port 48080"  dir=in action=allow protocol=TCP localport=48080

	containerizerPort := 48081
	gardenRunner := garden_runner.New("tcp4", gardenAddr, tmpDir, gardenBin, fmt.Sprintf("http://127.0.0.1:%d", containerizerPort))
	containerizerRunner := ginkgomon.New(ginkgomon.Config{
		Name:              "containerizer",
		Command:           exec.Command(containerizerBin, "127.0.0.1", strconv.Itoa(containerizerPort)),
		AnsiColorCode:     "",
		StartCheck:        "Control-C to quit.",
		StartCheckTimeout: 10 * time.Second,
		Cleanup:           func() {},
	})

	group := grouper.NewOrdered(syscall.SIGTERM, []grouper.Member{
		{Name: "containerizer", Runner: containerizerRunner},
		{Name: "garden", Runner: gardenRunner},
	})

	gardenProcess := ifrit.Invoke(group)

	return gardenProcess, gardenRunner.NewClient()
}

func StopGarden(process ifrit.Process, client garden.Client) {
	process.Signal(syscall.SIGKILL)
	Eventually(process.Wait(), 10).Should(Receive())
}
