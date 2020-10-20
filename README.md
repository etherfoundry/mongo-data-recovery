# MongoDB Data Recovery
A data recovery tool written in C# which consumes a MongoDB database (the .### and .ns files), in the MMAPv1 storage engine,
and attempts to retrieve deleted data from it.

There are 2 methods implemented:
- Traversing the 'deleted records' linked list in the namespace file
- Searching for a deleted record marker of 0xEEEEEEEE07

WiredTiger storage engine is not implemented.

*If you expect to use this, plan to adjust it according to your needs, it was written in a pinch.*

Methods influenced by "A method and tool to recover data deleted from a MongoDB (Jongseong Yoon, et al. 2017)"
