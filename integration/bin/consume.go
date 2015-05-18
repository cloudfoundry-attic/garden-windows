package main

import (
	"fmt"
	"math/rand"
	_ "net/http/pprof"
	"os"
	"os/exec"
	"runtime"
	"strconv"
	"time"

	"github.com/cloudfoundry/loggregator/src/bitbucket.org/kardianos/osext"
)

type ArrayBytes []byte

func bigBytes() ArrayBytes {
	time.Sleep(50 * time.Millisecond)
	s := make([]byte, 1024*1024)
	return s
}

func main() {
	if len(os.Args) == 1 {
		fmt.Println("Usage: consume [fork] [forkbomb|cpu [duration]|memory [megabytes]|disk [megabytes]]")
		os.Exit(1)
	}

	if os.Args[1] == "fork" {
		fork(os.Args[2:])
	} else if os.Args[1] == "memory" {
		generateMemoryLoad(os.Args[2])
	} else if os.Args[1] == "forkbomb" {
		forkbomb()
	} else if os.Args[1] == "disk" {
		generateDiskLoad(os.Args[2])
	} else {
		generateCPULoad(os.Args[2])
	}
}

func forkbomb() {
	filename, err := osext.Executable()
	if err != nil {
		panic(err)
	}
	done := make(chan interface{})
	runOne := func() {
		cmd := exec.Command(filename, "forkbomb")
		err = cmd.Run()
		if err != nil {
			panic(err)
		}
		close(done)
	}
	go runOne()
	go runOne()

	<-done
	os.Exit(0)
}

func fork(args []string) {
	filename, err := osext.Executable()
	if err != nil {
		panic(err)
	}
	cmd := exec.Command(filename, args...)
	err = cmd.Run()
	if err != nil {
		panic(err)
	}

	procState := cmd.ProcessState
	fmt.Printf("SystemTime: %d, UserTime: %d\r\n",
		procState.SystemTime(), procState.UserTime())
}

func generateCPULoad(duration string) {
	d, err := time.ParseDuration(duration)
	if err != nil {
		panic(err)
	}

	generateRands := func() {
		for {
			rand.Float64()
		}
	}
	maxProcs := runtime.NumCPU() * 40
	runtime.GOMAXPROCS(maxProcs)
	for i := 0; i < maxProcs; i++ {
		go generateRands()
	}

	time.Sleep(d)
	os.Exit(0)
}

func generateMemoryLoad(limit string) {
	var t []ArrayBytes
	var mem runtime.MemStats

	numMb, err := strconv.ParseInt(limit, 10, 64)
	if err != nil {
		fmt.Println("Usage: consume.exe [Num Megabytes to consume]")
		os.Exit(42)
	}

	for i := 0; i < int(numMb); i++ {
		runtime.ReadMemStats(&mem)
		fmt.Println("Consumed: ", mem.Alloc/1024/1024, "mb")

		s := bigBytes()
		if s == nil {
			fmt.Println("oh noes")
		}
		t = append(t, s)
	}

	fmt.Println("Memory Consumed Successfully")
	os.Exit(42)
}

func generateDiskLoad(limit string) {
	numMb, err := strconv.ParseInt(limit, 10, 64)
	if err != nil {
		fmt.Println("Usage: consume.exe disk [Num Megabytes to consume]")
		os.Exit(1)
	}

	onemb := make([]byte, 1024*1024)
	fh, err := os.Create("largefile.txt")
	if err != nil {
		panic(err)
	}
	defer fh.Close()
	for i := 0; i < int(numMb); i++ {
		_, err := fh.Write(onemb)
		if err != nil {
			panic(err)
		}
		fi, err := fh.Stat()
		if err != nil {
			panic(err)
		}
		fmt.Println("Consumed: ", fi.Size()/1024/1024, "mb")
	}

	fmt.Println("Disk Consumed Successfully")
	os.Exit(42)
}
