# Garden windows api limitation

| Method name                      | Implementation                                                      |
|----------------------------------|---------------------------------------------------------------------|
| Backend#Start                    | Not implemented                                                     |
| Backend#Stop                     | Not implemented                                                     |
| Backend#GraceTime                | Implemented                                                         |
| Backend#Ping                     | Implemented                                                         |
| Backend#Capacity                 | Implemented (MaxContainers = 100)                                   |
| Backend#Create                   | Implemented (except: `RootFSPath`, `ImageRef`, `Network` and the folowing limits: `Bandwidth`, `CPU` and `Pid`) |
| Backend#Destroy                  | Implemented                                                         |
| Backend#Containers               | Implemented                                                         |
| Backend#Lookup                   | Not Implemented (returns a dummy container without checking if the handle exists) |
| Backend#BulkInfo                 | Implemented -- (Only `MappedPorts`, `Properties` and `ExternalIP` are returned)   |
| Backend#BulkMetrics              | Implemented -- (Only, MemoryStat, CPUStat, DiskStat. No NetworkStat)              |
| Container#Handle                 | Implemented                                                                       |
| Container#Stop                   | Implemented (the `kill` flag is ignored, kill is the default)                     |
| Container#Info                   | Implemented (Only `MappedPorts`, `Properties` and `ExternalIP` are returned)      |
| Container#StreamIn               | Implemented                                                         |
| Container#StreamOut              | Implemented                                                         |
| Container#CurrentBandwidthLimits | Not Implemented                                                     |
| Container#CurrentCPULimits       | Not Implemented                                                     |
| Container#CurrentMemoryLimits    | Implemented                                                         |
| Container#NetIn                  | Implemented                                                         |
| Container#NetOut                 | Implemented -- (Except ICMP. ICMP is currently blocked for all containers) |
| Container#Run                    | (All flags are ignored except `Path`, `Args` and `Env`, see notes)    |
| Container#Attach                 | Not Implemented                                                       |
| Container#Metrics                | Implemented -- (Only, MemoryStat, CPUStat, DiskStat.  No NetworkStat) |
| Container#Properties             | Implemented                                                           |
| Container#Property               | Implemented                                                           |
| Container#SetProperty            | Implemented                                                           |
| Container#RemoveProperty         | Implemented                                                           |
| Container#SetGraceTime           | Implemented                                                           |
| Container#BulkNetOut             | Implemented                                                           |
| Container#CurrentDiskLimits      | Implemented                                                           |
| Container#LimitCPU               | Not Implemented (Not Part of API)                                     |
| Container#LimitDisk              | Implemented (Not Part of API)                                         |
| Container#LimitMemory            | Implemented (Not Part of API)                                         |
| Container#LimitBandwidth         | Not Implemented (Not Part of API)                                     |
| Processes#ID                     | Implemented                                                           |
| Processes#Wait                   | Implemented                                                           |
| Processes#SetTTY                 | Not Implemented                                                       |
| Processes#Signal                 | Implemented (the signal argument is ignored, the process is killed)   |

# Container

# Limitations of Container#Create

Supplying a base Docker image for your container via `RootFSPath` or `ImageRef` is not supported.
The following limits are ignored `Bandwidth`, `CPU` and `Pid`.  The `Pid` limit is hard-coded to 10.

# Limitations of Container#LimitCPU

CPU limits are not enforced.

# Limitations of Container#LimitBandwidth

Bandwidth limits are not enforced.

# Limitations of Container#Run

1. No support for `Privileged` contianers (the flag is ignored)
2. `User` flag is ignored (`garden-windows` creates a new user for each container)
3. Stdin is not supported (stdout/stderr enforce newlines)
