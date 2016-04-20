# ZipperBlobber

CLI application that can ZIP & BLOB a directory of files.

Originally built for easily storing a MongoDB dump in a blob in Azure, but it
can probably be used for other things as well.

## Example

Imagine that you have made a `mongodump` somewhere, e.g. in `C:\temp\dump`, and
now you would like to ZIP & BLOB the contents of that directory.

First you download & build ZipperBlobber. Then you run ZipperBlobber now by typing in

    > zipperblobber.exe

and it should look somewhat like this:

![/stuff/zb1.png]()

which means that it has a single command: `run`. Let's invoke it with the `run`
command:

    > zipperblobber run

which should result in a message saying that it needs three paramters:

![stuff/zb2.png]()

The `CONN` string next to the `storage` parameter means that it can pick up the
parameter from the `<connectionStrings>` section of the application configuration
file.

So if you try and edit the application configuration file `zipperblobber.exe.config` 
and set the appsetting `storage` to the connection string of the storage account that
you would like to use, running the `run` command again should now complain that
only two parameters are now missing:

    > zipperblobber run

![stuff/zb3.png]()

On my machine I had a MongoDB database dump in `D:\mongodumps\fm-events`, so I now
invoke the tool to ZIP & BLOB that directory and save it into a container called
`fm-events` in my storage account:

    > zipperblobber run -dir D:\mongodumps\fm-events -container fm-events

which resulted in this:

![stuff/zb4.png]()
