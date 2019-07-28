This directory contains modified implementation of the classes in [AvalonEdit](https://github.com/icsharpcode/AvalonEdit/tree/master/ICSharpCode.AvalonEdit/Rendering).

We want to use async Roslyn APIs, but the Avalon classes are not async.
Additionally, they mutate fields so we cannot just use "async void" as the mutation will run out of order.

There are two modifications to these classes

- Remove field mutation, and instead pass objects as parameters
- Convert overrides to async, and "async all the way up" to the top-level AsyncColorizingTransformer.Transform method, which is aync void
