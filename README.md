H5Ohm - the object to hierarchy mapper
=========================

hdf5 version
-------------------------

The build is currently being tested against hdf5 v1.8.16 as
this version is distributed by Ubuntu 16.01 LTS (xenial). 
However, hdf5 v1.10.x is also supported. In order for this
to work, define `HDF5_VER1_10` in the VisualStudio build 
options. This activates

- the `sw` and `mr` file modes for the SWMR feature
- use of `Int64` for the `hid_t`

