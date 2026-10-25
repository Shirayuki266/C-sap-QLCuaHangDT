var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory!=null&&!File.Exists(Path.Combine(directory.FullName,"PhoneStore.slnx")))directory=directory.Parent;
if(directory==null)throw new InvalidOperationException("Run inside PhoneStore repository.");
if(args.Contains("--product-images"))await ProductImages.Run(directory.FullName);
else if(args.Contains("--demo-data"))await DemoData.Run(directory.FullName);
else if(args.FirstOrDefault() is "--init" or "--serve" or "--cleanup" or "--verify")await QaEnvironment.Run(directory.FullName,args[0]);
else await new QaSuite(directory.FullName).Run(args.Contains("--children"),args.Contains("--browser-only"));

