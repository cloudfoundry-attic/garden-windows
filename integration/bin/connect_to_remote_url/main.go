package main

import (
	"fmt"
	"net"
	"os"

	"github.com/miekg/dns"
)

func main() {
	address := os.Getenv("ADDRESS")
	fmt.Println("requesting", address)
	if os.Getenv("PROTOCOL") == "udp" {
		udp(address)
	} else {
		tcp(address)
	}
}

func tcp(address string) {
	_, err := net.Dial("tcp", address)
	if err != nil {
		fmt.Println(err)
		os.Exit(1)
	}
	fmt.Printf("Connected successfully to %s", address)
}

func udp(address string) {
	m := new(dns.Msg)
	m.SetQuestion("google.com.", dns.TypeA)
	ret, err := dns.Exchange(m, address)
	if err != nil {
		fmt.Println(err)
		os.Exit(1)
	}
	if t, ok := ret.Answer[0].(*dns.A); ok {
		fmt.Println(t)
		fmt.Printf("Connected successfully to %s", address)
	}
}
