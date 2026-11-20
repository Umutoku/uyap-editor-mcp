param(
    [Parameter(Mandatory)][string]$Source,
    [Parameter(Mandatory)][string]$Output
)

$ErrorActionPreference = 'Stop'
$sourcePath = (Resolve-Path -LiteralPath $Source).Path
$outputPath = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $outputPath) { throw 'The synthetic output path already exists.' }

$rsa = [Security.Cryptography.RSA]::Create(2048)
$certificate = $null
$chain = $null
$archive = $null
try {
    $request = [Security.Cryptography.X509Certificates.CertificateRequest]::new(
        'CN=UYAP KURGU TEST - RESMI KULLANIM DISI', $rsa,
        [Security.Cryptography.HashAlgorithmName]::SHA256,
        [Security.Cryptography.RSASignaturePadding]::Pkcs1)
    $now = [DateTimeOffset]::UtcNow
    $certificate = $request.CreateSelfSigned($now.AddMinutes(-5), $now.AddDays(1))

    $archive = [IO.Compression.ZipFile]::OpenRead($sourcePath)
    if ($archive.GetEntry('sign.sgn')) { throw 'Use an unsigned synthetic UDF.' }
    $entry = $archive.GetEntry('content.xml')
    if (!$entry) { throw 'The synthetic UDF has no content.xml entry.' }
    $stream = $entry.Open()
    $buffer = [IO.MemoryStream]::new()
    try {
        $stream.CopyTo($buffer)
        $payload = $buffer.ToArray()
    } finally {
        $stream.Dispose()
        $buffer.Dispose()
    }
    $archive.Dispose()
    $archive = $null

    $content = [Security.Cryptography.Pkcs.ContentInfo]::new($payload)
    $cms = [Security.Cryptography.Pkcs.SignedCms]::new($content, $true)
    $signer = [Security.Cryptography.Pkcs.CmsSigner]::new($certificate)
    $signer.IncludeOption = [Security.Cryptography.X509Certificates.X509IncludeOption]::EndCertOnly
    [void]$signer.SignedAttributes.Add([Security.Cryptography.Pkcs.Pkcs9SigningTime]::new($now.UtcDateTime))
    $certificateHash = [Security.Cryptography.SHA256]::HashData($certificate.RawData)
    $signingCertificate = [Formats.Asn1.AsnWriter]::new([Formats.Asn1.AsnEncodingRules]::DER)
    [void]$signingCertificate.PushSequence()
    [void]$signingCertificate.PushSequence()
    [void]$signingCertificate.PushSequence()
    $signingCertificate.WriteOctetString($certificateHash)
    $signingCertificate.PopSequence()
    $signingCertificate.PopSequence()
    $signingCertificate.PopSequence()
    $attribute = [Security.Cryptography.AsnEncodedData]::new('1.2.840.113549.1.9.16.2.47',
        $signingCertificate.Encode())
    [void]$signer.SignedAttributes.Add($attribute)
    $cms.ComputeSignature($signer)
    $encoded = $cms.Encode()
    $verification = [Security.Cryptography.Pkcs.SignedCms]::new($content, $true)
    $verification.Decode($encoded)
    $verification.CheckSignature($true)

    $changed = [byte[]]$payload.Clone()
    $changed[0] = $changed[0] -bxor 1
    $changedContent = [Security.Cryptography.Pkcs.ContentInfo]::new($changed)
    $altered = [Security.Cryptography.Pkcs.SignedCms]::new($changedContent, $true)
    $altered.Decode($encoded)
    $tamperRejected = $false
    try { $altered.CheckSignature($true) }
    catch [Security.Cryptography.CryptographicException] { $tamperRejected = $true }
    if (!$tamperRejected) { throw 'A changed UDF payload unexpectedly passed signature verification.' }

    $chain = [Security.Cryptography.X509Certificates.X509Chain]::new()
    $chain.ChainPolicy.DisableCertificateDownloads = $true
    $chain.ChainPolicy.RevocationMode = [Security.Cryptography.X509Certificates.X509RevocationMode]::NoCheck
    if ($chain.Build($certificate)) { throw 'The synthetic certificate unexpectedly has Windows system trust.' }
    $chain.ChainPolicy.TrustMode = [Security.Cryptography.X509Certificates.X509ChainTrustMode]::CustomRootTrust
    [void]$chain.ChainPolicy.CustomTrustStore.Add($certificate)
    if (!$chain.Build($certificate)) { throw 'The synthetic certificate failed explicit local test trust.' }

    [IO.File]::Copy($sourcePath, $outputPath, $false)
    $archive = [IO.Compression.ZipFile]::Open($outputPath, [IO.Compression.ZipArchiveMode]::Update)
    $signature = $archive.CreateEntry('sign.sgn').Open()
    try { $signature.Write($encoded, 0, $encoded.Length) }
    finally { $signature.Dispose() }
    [pscustomobject]@{
        syntheticUdf = $outputPath
        detachedCmsVerified = $true
        modifiedContentRejected = $tamperRejected
        systemTrust = $false
        explicitlyTrustedInTest = $true
        certificateSubject = $certificate.Subject
    } | ConvertTo-Json -Compress
} finally {
    if ($archive) { $archive.Dispose() }
    if ($chain) { $chain.Dispose() }
    if ($certificate) { $certificate.Dispose() }
    $rsa.Dispose()
}
