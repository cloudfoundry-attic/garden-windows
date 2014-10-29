require './watcher_dot_net.rb'

#MIT License
#http://www.github.com/amirrajan/specwatchr
#Copyright (c) 2011 Amir Rajan, Matt Florence
#Copyright (c) 2011 The NSpec Development Team

=begin
 _______ _________ _______  _______ _________              _______  _______  _______ 
(  ____ \\__   __/(  ___  )(  ____ )\__   __/    |\     /|(  ____ \(  ____ )(  ____ \
| (    \/   ) (   | (   ) || (    )|   ) (       | )   ( || (    \/| (    )|| (    \/
| (_____    | |   | (___) || (____)|   | |       | (___) || (__    | (____)|| (__    
(_____  )   | |   |  ___  ||     __)   | |       |  ___  ||  __)   |     __)|  __)   
      ) |   | |   | (   ) || (\ (      | |       | (   ) || (      | (\ (   | (      
/\____) |   | |   | )   ( || ) \ \__   | |       | )   ( || (____/\| ) \ \__| (____/\
\_______)   )_(   |/     \||/   \__/   )_(       |/     \|(_______/|/   \__/(_______/

edit the code below to pick your builder and runner, 
for :builder you can use either :MSBuilder, or :RakeBuilder
for :test_runner you can use :NSpecRunner, :NUnitRunner, or :MSTestRunner

The default behavior: specwatchr will look for a file called Rakefile.rb, if it
finds one then it will automatically set the builder to rake builder
=end

config = { :builder => :MSBuilder, :test_runner => :NSpecRunner }

config[:builder] = :RakeBuilder if File.exists? "Rakefile.rb" #specwatchr will use :RakeBuilder if it finds Rakefile.rb

@dw = WatcherDotNet.new ".", config

=begin
 _______  _______  _______           _       
(  ____ \(  ____ )(  ___  )|\     /|( \      
| (    \/| (    )|| (   ) || )   ( || (      
| |      | (____)|| |   | || | _ | || |      
| | ____ |     __)| |   | || |( )| || |      
| | \_  )| (\ (   | |   | || || || || |      
| (___) || ) \ \__| (___) || () () || (____/\
(_______)|/   \__/(_______)(_______)(_______/

all notifications are faciltated throw Growl for Windows
=end

GrowlNotifier.growl_path_64 = 'C:\Program Files (x86)\Growl for Windows\growlnotify.exe'

GrowlNotifier.growl_path_32 = 'C:\Program Files\Growl for Windows\growlnotify.exe'

GrowlNotifier.growl_path = ""

GrowlNotifier.growl_path = GrowlNotifier.growl_path_32 if File.exists? GrowlNotifier.growl_path_32

GrowlNotifier.growl_path = GrowlNotifier.growl_path_64 if File.exists? GrowlNotifier.growl_path_64

=begin
 _        _______  _______  _______  _______ 
( (    /|(  ____ \(  ____ )(  ____ \(  ____ \
|  \  ( || (    \/| (    )|| (    \/| (    \/
|   \ | || (_____ | (____)|| (__    | |      
| (\ \) |(_____  )|  _____)|  __)   | |      
| | \   |      ) || (      | (      | |      
| )  \  |/\____) || )      | (____/\| (____/\
|/    )_)\_______)|/       (_______/(_______/

if you choose :NSpecRunner as your :test_runner,
this is the execution path for the NSpecRunner, the recommendation is that you install nspec via nuget: Install-Package nspec
if you do install from nuget, specwatchr will automatically find the file.
if you want to explicitly set the execution path for nspecrunner.exe, uncomment the line below
=end
#NSpecRunner.nspec_path = '.\packages\nspec.0.9.24\tools\nspecrunner.exe'

=begin
 _______  _______ _________ _______  _______ _________
(       )(  ____ \\__   __/(  ____ \(  ____ \\__   __/
| () () || (    \/   ) (   | (    \/| (    \/   ) (   
| || || || (_____    | |   | (__    | (_____    | |   
| |(_)| |(_____  )   | |   |  __)   (_____  )   | |   
| |   | |      ) |   | |   | (            ) |   | |   
| )   ( |/\____) |   | |   | (____/\/\____) |   | |   
|/     \|\_______)   )_(   (_______/\_______)   )_(   

if you choose :MSTestRunner as your :test_runner
this is the execution path for MSTest.exe
=end
MSTestRunner.ms_test_path = 
  'C:\program files (x86)\microsoft visual studio 10.0\common7\ide\mstest.exe'

=begin
 _                 _       __________________
( (    /||\     /|( (    /|\__   __/\__   __/
|  \  ( || )   ( ||  \  ( |   ) (      ) (   
|   \ | || |   | ||   \ | |   | |      | |   
| (\ \) || |   | || (\ \) |   | |      | |   
| | \   || |   | || | \   |   | |      | |   
| )  \  || (___) || )  \  |___) (___   | |   
|/    )_)(_______)|/    )_)\_______/   )_(   

if you choose :NUnitRunner as your :test_runner
this is the execution path for NUnit.exe
=end
NUnitRunner.nunit_path = 
  'C:\program files (x86)\nunit 2.5.9\bin\net-2.0\nunit-console-x86.exe'

=begin
 _______  _______  ______           _________ _        ______  
(       )(  ____ \(  ___ \ |\     /|\__   __/( \      (  __  \ 
| () () || (    \/| (   ) )| )   ( |   ) (   | (      | (  \  )
| || || || (_____ | (__/ / | |   | |   | |   | |      | |   ) |
| |(_)| |(_____  )|  __ (  | |   | |   | |   | |      | |   | |
| |   | |      ) || (  \ \ | |   | |   | |   | |      | |   ) |
| )   ( |/\____) || )___) )| (___) |___) (___| (____/\| (__/  )
|/     \|\_______)|/ \___/ (_______)\_______/(_______/(______/ 

if you choose :MSBuilder as your :builder
this is the execution path for MSBuild.exe
=end
MSBuilder.ms_build_path =
  'C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe'

=begin
 _______  _______  _        _______ 
(  ____ )(  ___  )| \    /\(  ____ \
| (    )|| (   ) ||  \  / /| (    \/
| (____)|| (___) ||  (_/ / | (__    
|     __)|  ___  ||   _ (  |  __)   
| (\ (   | (   ) ||  ( \ \ | (      
| ) \ \__| )   ( ||  /  \ \| (____/\
|/   \__/|/     \||_/    \/(_______/

if you choose :RakeBuilder as your :builder
this is the rake command that will get executed
=end
RakeBuilder.rake_command = 'rake'

=begin
 _______           _______  _______  _______ _________ ______   _______ 
(  ___  )|\     /|(  ____ \(  ____ )(  ____ )\__   __/(  __  \ (  ____ \
| (   ) || )   ( || (    \/| (    )|| (    )|   ) (   | (  \  )| (    \/
| |   | || |   | || (__    | (____)|| (____)|   | |   | |   ) || (__    
| |   | |( (   ) )|  __)   |     __)|     __)   | |   | |   | ||  __)   
| |   | | \ \_/ / | (      | (\ (   | (\ (      | |   | |   ) || (      
| (___) |  \   /  | (____/\| ) \ \__| ) \ \_____) (___| (__/  )| (____/\
(_______)   \_/   (_______/|/   \__/|/   \__/\_______/(______/ (_______/

specwathcr tries to automatically find your test dlls (it'll look for projects that end in the word Test, Tests, Spec or Specs)
if for some reason you deviate from this convention, you can OVERRIDE the dlls selected using the following line of code
=end
#@dw.test_runner.test_dlls = ['.\SampleSpecs\bin\Debug\SampleSpecs.dll']

=begin
 _______  _______  _______  _        _______ _________       _______           _______ 
(  ___  )(  ____ \(  ____ )( (    /|(  ____ \\__   __/      (       )|\     /|(  ____ \
| (   ) || (    \/| (    )||  \  ( || (    \/   ) (         | () () || )   ( || (    \/
| (___) || (_____ | (____)||   \ | || (__       | |         | || || || |   | || |      
|  ___  |(_____  )|  _____)| (\ \) ||  __)      | |         | |(_)| |( (   ) )| |      
| (   ) |      ) || (      | | \   || (         | |         | |   | | \ \_/ / | |      
| )   ( |/\____) || )    _ | )  \  || (____/\   | |         | )   ( |  \   /  | (____/\
|/     \|\_______)|/    (_)|/    )_)(_______/   )_(         |/     \|   \_/   (_______/

if you have the nuget package rake-dot-net installed you can use the following lines to build and deploy mvc applications everytime you save a web specific file
make sure to set your builder to :RakeBuilder
=end

#everything after this is specwatchr specific, feel free to dig into this, the source code for specwatchr is located in watcher_dot_net.rb
def handle filename
	@dw.consider filename
end

def handle_webfile filename
  if(@dw.config[:builder] == :RakeBuilder && File.exists?("RakeDotNet"))  #make sure that the configuration is set to RakeBuilder and RakeDotNet is installed
    failed = false #variable to determine if the file sync failed

    if(filename.match /App_Code/) #run the rake command if a web file in App_Code changed
      @dw.sh.execute RakeBuilder.rake_command
    else
      output = @dw.sh.execute "rake sync[\"#{ filename }\"]"  #run rake-dot-net's file sync command if any other web file changed
      
      failed = true if output =~ /rake aborted!/ #set failed equal to true if the sync failed
    end

    @dw.notifier.execute "website deployed", "deployed", "green" unless failed #growl

    #notify the dev that the version of rake they are using may be incorrect
    @dw.notifier.execute "sync failed", 
      "it looks like the sync failed, this usually happens if the version of rake you are running is NOT 0.8.7.  Please ensure you are running version 0.8.7 of rake. To see the gem versions that are installed, run the command 'gem list' in a command prompt that supports ruby.",
      "red" if failed
  else
    puts "A web file was encountered, but it looks like you don't have rake-dot-net installed.  I would auto deploy if you did."
  end
end

def tutorial
  @dw.notifier.execute "specwatchr", "feedback loop engaged", "green"
  puts "======================== SpecWatcher has started ==========================\n\n"
  puts "TEST RUNNER: #{@dw.test_runner.class}\n\n"
  puts "(you can change your test runner in dotnet.watchr.rb...)\n\n"

  if(@dw.test_runner.test_dlls.count == 0)
    puts "WARNING WARNING WARNING"
    puts "I didn't find any test projects.  Test projects MUST end in the word Test or Tests.  For example: UnitTests.csproj"
    puts "If you have these projects, try building your solution and re-running SpecWatchr\n\n"
  else
    puts "I have found the following test dll's in your solution:"
    @dw.test_runner.test_dlls.each { |dll| puts dll }
  end

  puts "\n\n"

  if(GrowlNotifier.growl_path != "")
    puts "GROWL PATH: #{GrowlNotifier.growl_path}"
  else
    puts "WARNING WARNING WARNING"
    puts "I didn't find Growl for Windows at path: #{ @growl32 } nor did I find it at #{ @growl64 }, make sure you have Growl for Windows installed.  If you have it installed elsewhere, update dotnet.watchr.rb accordingly."
  end

  puts "\n\n"

  puts "USAGE INSTRUCTIONS FOR #{@dw.test_runner.class}"
  puts @dw.test_runner.usage
end

def file_changed full_path
  if full_path =~ /.*.\.cs$/
    handle full_path
  end

  if full_path =~ /(.*.cshtml)|(.*.js)|(.*.css)$/
    handle_webfile full_path
  end
end

def tick i
  sleep(60)
  print i.to_s + ".."
  $stdout.flush
end

method_to_run = ARGV[0] #get the first argument from the command line and act accordingly

case method_to_run
when "tutorial" 
  tutorial
when "file_changed"
  file_changed ARGV[1].gsub("\\", "\/")[1..-1] #run the file_changed routine giving it a shell compatible file name
when "pomo_start"
  @dw.notifier.execute "pomodoro started", "25 mins left", "green"
  13.times { |i| tick(25 - (i + 1)) }
  @dw.notifier.execute "half way..", "12 mins left", "green"
  print "\a"
  12.times { |i| tick(25 - (i + 1 + 13)) }
  @dw.notifier.execute "done", "take a break", "red"
  print "\a"
  print "\a"
  print "\a"
when "pomo_break"
  @dw.notifier.execute "break started", "7 min left", "green"
  7.times { |i| tick (i + 1) }
  @dw.notifier.execute "back to it", "aww...", "red"
  print "\a"
  print "\a"
  print "\a"
else
  puts "I dont know how to run: " + method_to_run
end
