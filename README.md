# PooledMemoryStream
A drop-in replacement for MemoryStream which relies upon rented buffers from System.Buffers.ArrayPool. 

This is similar in purpose to Microsoft.IO.RecyclableMemoryStream.