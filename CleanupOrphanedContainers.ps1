
Import-Module WebAdministration

$sites = Get-ChildItem IIS:\Sites
$appPools = Get-ChildItem IIS:\AppPools

$sites |
	Where-Object { $_.Name -ne 'Containerizer' } |
	Remove-Website

$appPools |
	Where-Object { $_.Name -ne 'Containerizer' } |
	Remove-WebAppPool
