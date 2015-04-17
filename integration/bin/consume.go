package main

import (
	"fmt"
	_ "net/http/pprof"
	"os"
	"runtime"
	"strconv"
	"time"
)

type ArrayBytes []byte

func bigBytes() ArrayBytes {
	time.Sleep(50 * time.Millisecond)
	s := make([]byte, 1024*1024)
	return s
}

func main() {
	var t []ArrayBytes
	var mem runtime.MemStats

	numMb, err := strconv.ParseInt(os.Args[1], 10, 64)
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
