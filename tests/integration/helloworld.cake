var target = Argument("target", "Default");

Task("Default")
  .Does(ctx =>
{
  Information("Hello World!");
});

RunTarget(target);