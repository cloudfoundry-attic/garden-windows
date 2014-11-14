package container_test

import (
	. "github.com/onsi/ginkgo"
	. "github.com/onsi/gomega"

	"github.com/cloudfoundry-incubator/garden/api"
	netContainer "github.com/pivotal-cf-experimental/garden-dot-net/container"

	"io/ioutil"

	"strings"

	"code.google.com/p/go.net/websocket"
	"github.com/onsi/gomega/ghttp"

	"log"
	"net"
	"net/http"
	"net/url"
)

func uint64ptr(n uint64) *uint64 {
	return &n
}

var _ = Describe("backend", func() {
	var server *ghttp.Server
	var container api.Container

	BeforeEach(func() {
		server = ghttp.NewServer()
		u, _ := url.Parse(server.URL())
		container = netContainer.NewContainer(*u, "containerhandle")
	})

	AfterEach(func() {
		//shut down the server between tests
		if server.HTTPTestServer != nil {
			server.Close()
		}
	})

	Describe("StreamIn", func() {
		BeforeEach(func() {
			server.AppendHandlers(
				ghttp.CombineHandlers(
					ghttp.VerifyRequest("PUT", "/api/containers/containerhandle/files", "destination=a/path"),
					func(w http.ResponseWriter, req *http.Request) {
						body, err := ioutil.ReadAll(req.Body)
						req.Body.Close()
						Ω(err).ShouldNot(HaveOccurred())
						Ω(string(body)).Should(Equal("stuff"))
					},
				),
			)
		})

		It("makes a call out to an external service", func() {
			err := container.StreamIn("a/path", strings.NewReader("stuff"))
			Ω(err).NotTo(HaveOccurred())
			Ω(server.ReceivedRequests()).Should(HaveLen(1))
		})

	})

	Describe("StreamOut", func() {
		BeforeEach(func() {
			server.AppendHandlers(
				ghttp.CombineHandlers(
					ghttp.VerifyRequest("GET", "/api/containers/containerhandle/files", "source=a/path"),
					ghttp.RespondWith(200, "a tarball"),
				),
			)
		})

		It("makes a call out to an external service", func() {
			stream, err := container.StreamOut("a/path")
			Ω(err).NotTo(HaveOccurred())
			Ω(server.ReceivedRequests()).Should(HaveLen(1))

			body, err := ioutil.ReadAll(stream)
			Ω(err).NotTo(HaveOccurred())
			Ω(string(body)).Should(Equal("a tarball"))
		})

	})

	Describe("Running", func() {
		var events []api.ProcessSpec

		BeforeEach(func() {
			listener, err := net.Listen("tcp", ":2000")
			if err != nil {
				log.Fatal(err)
			}

			testHandler := func(ws *websocket.Conn) {
				for {
					var processSpec api.ProcessSpec
					websocket.JSON.Receive(ws, &processSpec)
					events = append(events, processSpec)
				}

			}
			http.Handle("/api/run", websocket.Handler(testHandler))

			go http.Serve(listener, nil)

			u, _ := url.Parse("http://localhost:2000")
			container = netContainer.NewContainer(*u, "containerhandle")
		})

		It("runs a script via a websocket and also passes rlimits ", func() {
			processSpec := api.ProcessSpec{
				Path: "/some/script",
				Args: []string{"arg1", "arg2"},
				Limits: api.ResourceLimits{
					As:         uint64ptr(1),
					Core:       uint64ptr(2),
					Cpu:        uint64ptr(3),
					Data:       uint64ptr(4),
					Fsize:      uint64ptr(5),
					Locks:      uint64ptr(6),
					Memlock:    uint64ptr(7),
					Msgqueue:   uint64ptr(8),
					Nice:       uint64ptr(9),
					Nofile:     uint64ptr(10),
					Nproc:      uint64ptr(11),
					Rss:        uint64ptr(12),
					Rtprio:     uint64ptr(13),
					Sigpending: uint64ptr(14),
					Stack:      uint64ptr(15),
				},
			}
			_, err := container.Run(processSpec, api.ProcessIO{})

			Ω(err).ShouldNot(HaveOccurred())
			Eventually(func() []api.ProcessSpec {
				return events
			}).Should(ContainElement(processSpec))

			// ranCmd, _, _ := fakeProcessTracker.RunArgstmForCall(0)
			// Ω(ranCmd.Path).Should(Equal(containerDir + "/bin/wsh"))

			// Ω(ranCmd.Args).Should(Equal([]string{
			// 	containerDir + "/bin/wsh",
			// 	"--socket", containerDir + "/run/wshd.sock",
			// 	"--user", "vcap",
			// 	"--env", "env1=env1Value",
			// 	"--env", "env2=env2Value",
			// 	"/some/script",
			// 	"arg1",
			// 	"arg2",
			// }))

			// Ω(ranCmd.Env).Should(Equal([]string{
			// 	"RLIMIT_AS=1",
			// 	"RLIMIT_CORE=2",
			// 	"RLIMIT_CPU=3",
			// 	"RLIMIT_DATA=4",
			// 	"RLIMIT_FSIZE=5",
			// 	"RLIMIT_LOCKS=6",
			// 	"RLIMIT_MEMLOCK=7",
			// 	"RLIMIT_MSGQUEUE=8",
			// 	"RLIMIT_NICE=9",
			// 	"RLIMIT_NOFILE=10",
			// 	"RLIMIT_NPROC=11",
			// 	"RLIMIT_RSS=12",
			// 	"RLIMIT_RTPRIO=13",
			// 	"RLIMIT_SIGPENDING=14",
			// 	"RLIMIT_STACK=15",
			// }))
		})

		// It("runs the script with environment variables", func() {
		// 	_, err := container.Run(api.ProcessSpec{
		// 		Path: "/some/script",
		// 		Env:  []string{"ESCAPED=kurt \"russell\"", "UNESCAPED=isaac\nhayes"},
		// 	}, api.ProcessIO{})

		// 	Ω(err).ShouldNot(HaveOccurred())

		// 	ranCmd, _, _ := fakeProcessTracker.RunArgsForCall(0)
		// 	Ω(ranCmd.Args).Should(Equal([]string{
		// 		containerDir + "/bin/wsh",
		// 		"--socket", containerDir + "/run/wshd.sock",
		// 		"--user", "vcap",
		// 		"--env", "env1=env1Value",
		// 		"--env", "env2=env2Value",
		// 		"--env", `ESCAPED=kurt "russell"`,
		// 		"--env", "UNESCAPED=isaac\nhayes",
		// 		"/some/script",
		// 	}))
		// })

		// It("runs the script with the working dir set if present", func() {
		// 	_, err := container.Run(api.ProcessSpec{
		// 		Path: "/some/script",
		// 		Dir:  "/some/dir",
		// 	}, api.ProcessIO{})

		// 	Ω(err).ShouldNot(HaveOccurred())

		// 	ranCmd, _, _ := fakeProcessTracker.RunArgsForCall(0)
		// 	Ω(ranCmd.Args).Should(Equal([]string{
		// 		containerDir + "/bin/wsh",
		// 		"--socket", containerDir + "/run/wshd.sock",
		// 		"--user", "vcap",
		// 		"--env", "env1=env1Value",
		// 		"--env", "env2=env2Value",
		// 		"--dir", "/some/dir",
		// 		"/some/script",
		// 	}))
		// })

		// It("runs the script with a TTY if present", func() {
		// 	ttySpec := &api.TTYSpec{
		// 		WindowSize: &api.WindowSize{
		// 			Columns: 123,
		// 			Rows:    456,
		// 		},
		// 	}

		// 	_, err := container.Run(api.ProcessSpec{
		// 		Path: "/some/script",
		// 		TTY:  ttySpec,
		// 	}, api.ProcessIO{})

		// 	Ω(err).ShouldNot(HaveOccurred())

		// 	_, _, tty := fakeProcessTracker.RunArgsForCall(0)
		// 	Ω(tty).Should(Equal(ttySpec))
		// })

		// Describe("streaming", func() {
		// 	JustBeforeEach(func() {
		// 		fakeProcessTracker.RunStub = func(cmd *exec.Cmd, io api.ProcessIO, tty *api.TTYSpec) (api.Process, error) {
		// 			writing := new(sync.WaitGroup)
		// 			writing.Add(1)

		// 			go func() {
		// 				defer writing.Done()
		// 				defer GinkgoRecover()

		// 				_, err := fmt.Fprintf(io.Stdout, "hi out\n")
		// 				Ω(err).ShouldNot(HaveOccurred())

		// 				_, err = fmt.Fprintf(io.Stderr, "hi err\n")
		// 				Ω(err).ShouldNot(HaveOccurred())
		// 			}()

		// 			process := new(wfakes.FakeProcess)

		// 			process.IDReturns(42)

		// 			process.WaitStub = func() (int, error) {
		// 				writing.Wait()
		// 				return 123, nil
		// 			}

		// 			return process, nil
		// 		}
		// 	})

		// 	It("streams stderr and stdout and exit status", func() {
		// 		stdout := gbytes.NewBuffer()
		// 		stderr := gbytes.NewBuffer()

		// 		process, err := container.Run(api.ProcessSpec{
		// 			Path: "/some/script",
		// 		}, api.ProcessIO{
		// 			Stdout: stdout,
		// 			Stderr: stderr,
		// 		})
		// 		Ω(err).ShouldNot(HaveOccurred())

		// 		Ω(process.ID()).Should(Equal(uint32(42)))

		// 		Eventually(stdout).Should(gbytes.Say("hi out\n"))
		// 		Eventually(stderr).Should(gbytes.Say("hi err\n"))

		// 		Ω(process.Wait()).Should(Equal(123))
		// 	})
		// })

		// It("only sets the given rlimits", func() {
		// 	_, err := container.Run(api.ProcessSpec{
		// 		Path: "/some/script",
		// 		Limits: api.ResourceLimits{
		// 			As:      &1,
		// 			Cpu:     &3,
		// 			Fsize:   &5,
		// 			Memlock: &7,
		// 			Nice:    &9,
		// 			Nproc:   &11,
		// 			Rtprio:  &13,
		// 			Stack:   &15,
		// 		},
		// 	}, api.ProcessIO{})

		// 	Ω(err).ShouldNot(HaveOccurred())

		// 	ranCmd, _, _ := fakeProcessTracker.RunArgsForCall(0)
		// 	Ω(ranCmd.Path).Should(Equal(containerDir + "/bin/wsh"))

		// 	Ω(ranCmd.Args).Should(Equal([]string{
		// 		containerDir + "/bin/wsh",
		// 		"--socket", containerDir + "/run/wshd.sock",
		// 		"--user", "vcap",
		// 		"--env", "env1=env1Value",
		// 		"--env", "env2=env2Value",
		// 		"/some/script",
		// 	}))

		// 	Ω(ranCmd.Env).Should(Equal([]string{
		// 		"RLIMIT_AS=1",
		// 		"RLIMIT_CPU=3",
		// 		"RLIMIT_FSIZE=5",
		// 		"RLIMIT_MEMLOCK=7",
		// 		"RLIMIT_NICE=9",
		// 		"RLIMIT_NPROC=11",
		// 		"RLIMIT_RTPRIO=13",
		// 		"RLIMIT_STACK=15",
		// 	}))
		// })

		// Context("with 'privileged' true", func() {
		// 	It("runs with --user root", func() {
		// 		_, err := container.Run(api.ProcessSpec{
		// 			Path:       "/some/script",
		// 			Privileged: true,
		// 		}, api.ProcessIO{})

		// 		Ω(err).ToNot(HaveOccurred())

		// 		ranCmd, _, _ := fakeProcessTracker.RunArgsForCall(0)
		// 		Ω(ranCmd.Path).Should(Equal(containerDir + "/bin/wsh"))

		// 		Ω(ranCmd.Args).Should(Equal([]string{
		// 			containerDir + "/bin/wsh",
		// 			"--socket", containerDir + "/run/wshd.sock",
		// 			"--user", "root",
		// 			"--env", "env1=env1Value",
		// 			"--env", "env2=env2Value",
		// 			"/some/script",
		// 		}))
		// 	})
		// })

		// Context("when spawning fails", func() {
		// 	disaster := errors.New("oh no!")

		// 	JustBeforeEach(func() {
		// 		fakeProcessTracker.RunReturns(nil, disaster)
		// 	})

		// 	It("returns the error", func() {
		// 		_, err := container.Run(api.ProcessSpec{
		// 			Path:       "/some/script",
		// 			Privileged: true,
		// 		}, api.ProcessIO{})
		// 		Ω(err).Should(Equal(disaster))
		// 	})
		// })
	})
})
