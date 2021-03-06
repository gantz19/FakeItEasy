require 'albacore'

msbuild_command = "C:/Program Files (x86)/MSBuild/14.0/Bin/MSBuild.exe"
if !File.file?(msbuild_command)
  raise "MSBuild not found"
end

nuget_command   = ".nuget/nuget.exe"
gitlink_command = "packages/gitlink.2.3.0/lib/net45/GitLink.exe"
xunit_command   = "packages/xunit.runner.console.2.0.0/tools/xunit.console.exe"

solution        = "FakeItEasy.sln"
assembly_info   = "src/CommonAssemblyInfo.cs"
version         = IO.read(assembly_info)[/AssemblyInformationalVersion\("([^"]+)"\)/, 1]
version_suffix  = ENV["VERSION_SUFFIX"]
build           = (ENV["BUILD"] || "").rjust(6, "0");
build_suffix    = version_suffix.to_s.empty? ? "" : "-build" + build;
repo_url        = "https://github.com/FakeItEasy/FakeItEasy"
nuspec          = "src/FakeItEasy.nuspec"
analyzer_nuspec = "src/FakeItEasy.Analyzer.nuspec"
logs            = "artifacts/logs"
output          = File.absolute_path("artifacts/output")
tests           = "artifacts/tests"
packages        = File.absolute_path("packages")

gitlinks        = ["FakeItEasy", "FakeItEasy.Analyzer.CSharp", "FakeItEasy.Analyzer.VisualBasic", "FakeItEasy.netstd"]

unit_tests = [
  "tests/FakeItEasy.Tests/bin/Release/FakeItEasy.Tests.dll",
  "tests/FakeItEasy.Analyzer.CSharp.Tests/bin/Release/FakeItEasy.Analyzer.CSharp.Tests.dll",
  "tests/FakeItEasy.Analyzer.VisualBasic.Tests/bin/Release/FakeItEasy.Analyzer.VisualBasic.Tests.dll"
]

netstd_unit_test_directories = [
  "tests/FakeItEasy.Tests.netstd"
]

integration_tests = [
  "tests/FakeItEasy.IntegrationTests/bin/Release/FakeItEasy.IntegrationTests.dll",
  "tests/FakeItEasy.IntegrationTests.VB/bin/Release/FakeItEasy.IntegrationTests.VB.dll"
]

netstd_integration_test_directories = [
  "tests/FakeItEasy.IntegrationTests.netstd"
]

specs = [
  "tests/FakeItEasy.Specs/bin/Release/FakeItEasy.Specs.dll"
]

netstd_spec_directories = [
  "tests/FakeItEasy.Specs.netstd"
]

approval_tests = [
  "tests/FakeItEasy.Tests.Approval/bin/Release/FakeItEasy.Tests.Approval.dll",
  "tests/FakeItEasy.Tests.Approval.netstd/bin/Release/FakeItEasy.Tests.Approval.netstd.dll"
]

repo = 'FakeItEasy/FakeItEasy'
appveyor_server_url = 'https://ci.appveyor.com/project/FakeItEasy/fakeiteasy/settings'
release_issue_labels = ['P2', 'build', 'documentation']
release_issue_rtm_steps = <<-eos
Can be labelled **ready** when all other issues on this milestone are closed.

- [ ] run code analysis in VS in *Release* mode and address violations (send a regular PR which must be merged before continuing)
- [ ] change `VERSION_SUFFIX` in [AppVeyor](#{appveyor_server_url}) to "" and initiate a build
- [ ] check build
- edit draft release in [GitHub UI](https://github.com/FakeItEasy/FakeItEasy/releases):
    - [ ] ensure completeness of release notes, including non-owner contributors, if any (move release notes forward from any pre-releases to the current release)
    - [ ] attach main nupkg and analyzer nupkg
    - [ ] publish the release
- [ ] push nupkgs to NuGet using the Deploy option on the AppVeyor build
- [ ] tweet, mentioning contributors
- [ ] post link to tweet as comment here for easy retweeting ;-)
- [ ] post tweet in [Gitter](https://gitter.im/FakeItEasy/FakeItEasy)
- [ ] post a link to the GitHub Release in each issue in this milestone, with thanks to contributors
- [ ] run `build.cmd next_version[new_version]` to
    - create a pull request that changes the version in CommonAssemblyInfo.cs to the expected version (of form _xx.yy.zz_)
    - create a new draft GitHub Release
    - create a new milestone for the next release
    - create a new issue (like this one) for the next release, adding it to the new milestone
- [ ] close this milestone
- [ ] close this issue
eos

release_issue_prerelease_steps = <<-eos
Can be labelled **ready** when all other issues destined for this release are closed.

- [ ] run code analysis in VS in *Release* mode and address violations (send a regular PR which must be merged before continuing)
- [ ] change `VERSION_SUFFIX` in [AppVeyor](#{appveyor_server_url}) to "-%version_suffix%" and initiate a build
- [ ] check build
- edit draft release in [GitHub UI](https://github.com/FakeItEasy/FakeItEasy/releases):
    - [ ] ensure completeness of release notes, including non-owner contributors, if any
    - [ ] attach main nupkg and analyzer nupkg
    - [ ] publish the release
- [ ] push nupkgs to NuGet using the Deploy option on the AppVeyor build
- [ ] tweet, mentioning contributors
- [ ] post link to tweet as comment here for easy retweeting ;-)
- [ ] post tweet in [Gitter](https://gitter.im/FakeItEasy/FakeItEasy)
- [ ] if another pre-release is planned, use the `pre_release` task (e.g. `build.cmd pre_release[beta002]`) to
    - create a new draft GitHub Release
    - create a new issue (like this one) for the next release, adding it to the next RTM milestone
- [ ] close this issue
eos

release_body = <<-eos
### Changed
* _&lt;Description of change.&gt;_ (#_&lt;issue number&gt;_)

### New
* _&lt;Description of feature.&gt;_ (#_&lt;issue number&gt;_)

### Fixed
* _&lt;Description of fix.&gt;_ (#_&lt;issue number&gt;_)

### With special thanks for contributions to this release from:
* _&lt;user's actual name&gt;_ - @_&lt;github userid&gt;_
eos

ssl_cert_filename = "cacert.pem"

Albacore.configure do |config|
  config.log_level = :verbose
end

desc "Execute default tasks"
task :default => [ :vars, :gitlink, :unit, :integ, :spec, :approve, :pack ]

desc "Print all variables"
task :vars do
  print_vars(local_variables.sort.map { |name| [name.to_s, (eval name.to_s)] })
end

desc "Restore NuGet packages"
task :restore do
  restore_cmd = Exec.new
  restore_cmd.command = nuget_command
  restore_cmd.parameters "restore #{solution} -PackagesDirectory #{packages}"
  restore_cmd.execute

  # performing restore on the solution doesn't restore
  # all the xprojs' dependencies, so do them separately
  (netstd_unit_test_directories + netstd_integration_test_directories + netstd_spec_directories).each do | test_directory |
    restore_dependencies_for_project test_directory
  end
end

directory logs

desc "Clean solution"
task :clean => [logs] do
  run_msbuild solution, "Clean", msbuild_command
end

desc "Update version number and create pull request, milestone, release, and release checklist issue"
task :next_version, :new_version do |asm, args|
  new_version = args.new_version or
    fail "ERROR: A new version is required, e.g.: build.cmd next_version[2.3.0]"

  current_branch = `git rev-parse --abbrev-ref HEAD`.strip()

  if current_branch != 'master'
    fail "ERROR: Current branch is '#{current_branch}'. Must be on branch 'master' to set new version."
  end if

  new_branch = "set-version-to-" + new_version

  require 'octokit'

  use_ssl_cert_file(ssl_cert_filename)

  client = Octokit::Client.new(:netrc => true)

  puts "Creating branch '#{new_branch}'..."
  `git checkout -b #{new_branch}`
  puts "Created branch '#{new_branch}'."

  puts "Setting version to '#{new_version}' in '#{assembly_info}'..."
  Rake::Task["set_version_in_assemblyinfo"].invoke(new_version)
  puts "Set version to '#{new_version}' in '#{assembly_info}'."

  puts "Committing '#{assembly_info}'..."
  `git commit -m "setting version to #{new_version}" #{assembly_info}`
  puts "Committed '#{assembly_info}'."

  puts "Pushing '#{new_branch}' to origin..."
  `git push origin #{new_branch}"`
  puts "Pushed '#{new_branch}' to origin."

  puts "Creating pull request..."
  pull_request = client.create_pull_request(
    repo,
    "master",
    "#{client::user.login}:#{new_branch}",
    "set version to #{new_version} for next release",
    "preparing for #{new_version}"
  )
  puts "Created pull request \##{pull_request.number} '#{pull_request.title}'."

  puts "Creating milestone '#{new_version}'..."
  milestone = client.create_milestone(
    repo,
    new_version,
    :description => new_version + ' release'
    )
  puts "Created milestone '#{new_version}'."

  create_release(
                 client,
                 repo,
                 milestone,
                 new_version,
                 release_body,
                 release_issue_rtm_steps,
                 release_issue_labels)
end

desc "Create GitHub Release and release checklist issue for pre-release build"
task :pre_release, :version_suffix do |asm, args|
  version_suffix = args.version_suffix or
    fail 'ERROR: A version suffix is required, e.g.: build.cmd pre_release[beta001]'

  require 'octokit'

  use_ssl_cert_file(ssl_cert_filename)

  client = Octokit::Client.new(:netrc => true)

  milestone = client
    .list_milestones(repo, :state => 'open')
    .select { |m| m.title == version }
    .first or
    raise "ERROR: can't find existing milestone #{version}"

  create_release(
                 client,
                 repo,
                 milestone,
                 "#{version}-#{version_suffix}",
                 release_body,
                 release_issue_prerelease_steps.sub('%version_suffix%', version_suffix),
                 release_issue_labels)
end

desc "Update assembly info"
assemblyinfo :set_version_in_assemblyinfo, :new_version do |asm, args|
  new_version = args.new_version

  # not using asm.version and asm.file_version due to StyleCop violations
  asm.custom_attributes = {
    :AssemblyVersion => new_version,
    :AssemblyFileVersion => new_version,
    :AssemblyInformationalVersion => new_version
  }
  asm.input_file = assembly_info
  asm.output_file = assembly_info
end

desc "Build solution"
task :build => [:clean, :restore, logs] do
  run_msbuild solution, "Build", msbuild_command, packages
end

desc "GitLink PDB's"
exec :gitlink => [:build] do |cmd|
  cmd.command = gitlink_command
  cmd.parameters ". -f #{solution} -u #{repo_url} -include " + gitlinks.join(",")
end

directory tests

desc "Execute unit tests"
task :unit => [:build, tests] do
    run_tests(unit_tests, xunit_command, tests)
    run_netstd_tests(netstd_unit_test_directories, tests)
end

desc "Execute integration tests"
task :integ => [:build, tests] do
    run_tests(integration_tests, xunit_command, tests)
    run_netstd_tests(netstd_integration_test_directories, tests)
end

desc "Execute specifications"
task :spec => [:build, tests] do
    run_tests(specs, xunit_command, tests)
    run_netstd_tests(netstd_spec_directories, tests)
end

desc "Execute approval tests"
task :approve => [:build, tests] do
    run_tests(approval_tests, xunit_command, tests)
end

directory output

desc "create the nuget package"
exec :pack => [:build, output] do |cmd|
  cmd.command = nuget_command
  cmd.parameters "pack #{nuspec} -Version #{version}#{version_suffix}#{build_suffix} -OutputDirectory #{output}"
end

desc "create the analyzer nuget package"
exec :pack => [:build, output] do |cmd|
  cmd.command = nuget_command
  cmd.parameters "pack #{analyzer_nuspec} -Version #{version}#{version_suffix}#{build_suffix} -OutputDirectory #{output}"
end

def create_release(client, repo, milestone, release_version, release_body, release_issue_body, release_issue_labels)
  release_description = release_version + ' release'
  puts "Creating issue '#{release_description}'..."
  issue = client.create_issue(
    repo,
    release_description,
    release_issue_body,
    :labels => release_issue_labels,
    :milestone => milestone.number
    )
  puts "Created issue \##{issue.number} '#{release_description}'."

  puts "Creating release '#{release_version}'..."
  client.create_release(
    repo,
    release_version,
    :name => release_version,
    :draft => true,
    :body => release_body
    )
  puts "Created release '#{release_version}'."
end

def print_vars(variables)

  scalars = []
  vectors = []

  variables.each { |name, value|
    if value.respond_to?('each')
      vectors << [name, value.map { |v| v.to_s }]
    else
      string_value = value.to_s
      lines = string_value.lines
      if lines.length > 1
        vectors << [name, lines]
      else
        scalars << [name, string_value]
      end
    end
  }

  scalar_name_column_width = scalars.map { |s| s[0].length }.max
  scalars.each { |name, value|
    puts "#{name}:#{' ' * (scalar_name_column_width - name.length)} #{value}"
  }

  puts
  vectors.select { |name, value| ![
                                   'release_body',
                                   'release_issue_rtm_steps',
                                   'release_issue_prerelease_steps',
                                   'release_issue_labels'
                                  ].include? name }.each { |name, value|
    puts "#{name}:"
    puts value.map {|v| "  " + v }
    puts ""
  }
end

def run_msbuild(solution, target, command, packages_dir = nil)
  packages_dir_option = "/p:NuGetPackagesDirectory=#{packages_dir}" if packages_dir
  cmd = Exec.new
  cmd.command = command
  cmd.parameters "#{solution} /target:#{target} /p:configuration=Release /nr:false /verbosity:minimal /nologo /fl /flp:LogFile=artifacts/logs/#{target}.log;Verbosity=Detailed;PerformanceSummary #{packages_dir_option}"
  cmd.execute
end

def run_tests(test_assemblies, command, result_dir)
  test_assemblies.each do |test_assembly|
    xml   = File.expand_path(File.join(result_dir, File.basename(test_assembly, '.dll') + '.TestResults.xml'))
    html  = File.expand_path(File.join(result_dir, File.basename(test_assembly, '.dll') + '.TestResults.html'))

    xunit = XUnitTestRunner.new
    xunit.command = command
    xunit.assembly = test_assembly
    xunit.options '-noshadow', '-nologo', '-notrait', '"explicit=yes"', '-xml', xml, '-html', html
    xunit.execute
  end
end

def run_netstd_tests(test_directories, result_dir)
  test_directories.each do |test_directory|
    xml = File.expand_path(File.join(result_dir, File.basename(test_directory) + '.TestResults.xml'))
    # the xunit .NET Core runner can't produce HTML yet since XSLT isn't scheduled until .NET Core 1.2

    test_cmd = Exec.new
    test_cmd.command = "dotnet"
    test_cmd.parameters "test -c Release #{test_directory} -nologo -notrait \"explicit=yes\" -xml #{xml}"
    test_cmd.execute
  end
end

def restore_dependencies_for_project(project_dir)
  restore_cmd = Exec.new
  restore_cmd.command = "dotnet"
  restore_cmd.parameters "restore #{project_dir}"
  restore_cmd.execute
end

# Load SSL cert file to enable Ruby SSL support.
# New cert files can be had from
#    http://curl.haxx.se/ca/cacert.pem
# in case the current one stops working.
def use_ssl_cert_file(ssl_cert_filename)
  ENV["SSL_CERT_FILE"] = File.absolute_path(ssl_cert_filename)
end
