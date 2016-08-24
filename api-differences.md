# Garden windows api limitation

| Method name                      | Implementation                                                      |
|----------------------------------|---------------------------------------------------------------------|
| Backend#Start                    | Not implmented                                                      |
| Backend#Stop                     | Not implemented                                                     |
| Backend#GraceTime                | Not implemented                                                     |
| Backend#Ping                     |                                                                     |
| Backend#Capacity                 | (MaxContainers = 256)                                               |
| Backend#Create                   | (All container specs are ignored except `Handle` and `Properties`)  |
| Backend#Destroy                  | Implemented                                                         |
| Backend#Containers               | Implemented                                                         |
| Backend#Lookup                   | (dummy ; returns a container without checking if the handle exists) |
| Backend#BulkInfo                 | Implemented                                                         |
| Backend#BulkMetrics              | In progress                                                         |
| Container#Handle                 | Implemented                                                         |
| Container#Stop                   | (the `kill` flag is ignored, kill is the default)                   |
| Container#Info                   | (Only `MappedPorts`, `Properties` and `ExternalIP` are returned)    |
| Container#StreamIn               | Implemented                                                         |
| Container#StreamOut              | Implemented                                                         |
| Container#LimitBandwidth         | Not Implemented                                                     |
| Container#CurrentBandwidthLimits | Not Implemented                                                     |
| Container#LimitCPU               | In progess (see notes below)                                        |
| Container#CurrentCPULimits       | In progress                                                         |
| Container#LimitDisk              | In progress                                                         |
| Container#CurrentCPULimits       | In progress                                                         |
| Container#LimitMemroy            | Implemented                                                         |
| Container#CurrentMemoryLimits    | Implemented                                                         |
| Container#NetIn                  | (TODO: what are the limitations ?)                                  |
| Container#NetOut                 | (Except ICMP. ICMP is currently blocked for all containers)         |
| Container#Run                    | (All flags are ignored except `Path`, `Args` and `Env`, see notes)  |
| Container#Attach                 | Not implemented                                                     |
| Container#Metrics                | In progress (TODO: document the limitations)                        |
| Container#Properties             | Implemented                                                         |
| Container#Property               | Implemented                                                         |
| Container#SetProperty            | Implemented                                                         |
| Container#RemoveProperty         | Implemented                                                         |
| Processes#ID                     | Implemented                                                         |
| Processes#Wait                   | Implemented                                                         |
| Processes#SetTTY                 | Not Implemented                                                     |
| Processes#Signal                 | Implemented (the signal argument is ignored, the process is killed) |

# Limitations of Container#Create

Supplying a base Docker image for your container via `RootFSPath` is not supported.

# Limitations of Container#LimitCPU

CPU limits are not currently enforced.

# Limitations of Container#Run

1. No support for `Privileged` contianers (the flag is ignored)
2. `User` flag is ignored (`garden-windows` creates a new user for each container)
3. Stdin is not supported (stdout/stderr enforce newlines)
