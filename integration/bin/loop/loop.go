package main

import (
	"fmt"
	"time"
)

func main() {
	for {
		time.Sleep(1 * time.Second)
		fmt.Println("I'm alive")
	}
}
