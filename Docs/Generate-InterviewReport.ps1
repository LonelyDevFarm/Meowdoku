# Tao file Word (.docx) chuan OOXML tu file noi dung InterviewReport.content.txt
# Khong can Word/Office dang chay - tu build cau truc zip/XML cua .docx.
# Chay: powershell -NoProfile -ExecutionPolicy Bypass -File "Generate-InterviewReport.ps1"

$ContentPath = Join-Path $PSScriptRoot "InterviewReport.content.txt"
$OutputPath  = Join-Path $PSScriptRoot "Meowdoku_BaoCao_PhongVan.docx"
$TempDir     = Join-Path $PSScriptRoot "_docx_build_tmp"

if (Test-Path $TempDir) { Remove-Item $TempDir -Recurse -Force }
New-Item -ItemType Directory -Path $TempDir | Out-Null
New-Item -ItemType Directory -Path (Join-Path $TempDir "_rels") | Out-Null
New-Item -ItemType Directory -Path (Join-Path $TempDir "word\_rels") | Out-Null
New-Item -ItemType Directory -Path (Join-Path $TempDir "docProps") | Out-Null

function Escape-Xml([string]$text) {
    $text = $text -replace "&", "&amp;"
    $text = $text -replace "<", "&lt;"
    $text = $text -replace ">", "&gt;"
    $text = $text -replace '"', "&quot;"
    return $text
}

$bodyBuilder = New-Object System.Text.StringBuilder

function Add-Paragraph([string]$styleId, [string]$text, [string]$fontName) {
    $escaped = Escape-Xml $text
    $rpr = ""
    if ($fontName) {
        $rpr = "<w:rFonts w:ascii=`"$fontName`" w:hAnsi=`"$fontName`"/><w:sz w:val=`"20`"/>"
    }
    $pStyle = if ($styleId) { "<w:pStyle w:val=`"$styleId`"/>" } else { "" }
    [void]$bodyBuilder.Append("<w:p><w:pPr>$pStyle</w:pPr><w:r>")
    if ($rpr) { [void]$bodyBuilder.Append("<w:rPr>$rpr</w:rPr>") }
    [void]$bodyBuilder.Append("<w:t xml:space=`"preserve`">$escaped</w:t></w:r></w:p>")
}

$lines = Get-Content -LiteralPath $ContentPath -Encoding UTF8
foreach ($line in $lines) {
    if ($line -eq "") { Add-Paragraph "" "" $null; continue }
    $idx = $line.IndexOf("|")
    if ($idx -lt 0) { continue }
    $prefix = $line.Substring(0, $idx)
    $text = $line.Substring($idx + 1)
    switch ($prefix) {
        "T"  { Add-Paragraph "Title" $text $null }
        "H1" { Add-Paragraph "Heading1" $text $null }
        "H2" { Add-Paragraph "Heading2" $text $null }
        "B"  { Add-Paragraph "ListParagraph" ("- " + $text) $null }
        "N1" { Add-Paragraph "Normal" $text $null }
        "C"  { Add-Paragraph "Normal" $text "Consolas" }
        default { Add-Paragraph "Normal" $text $null }
    }
}

Write-Output "Generated $($lines.Count) content lines into document body."

$documentXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
<w:body>
$($bodyBuilder.ToString())
<w:sectPr><w:pgSz w:w="11906" w:h="16838"/><w:pgMar w:top="1134" w:right="1134" w:bottom="1134" w:left="1134" w:header="708" w:footer="708" w:gutter="0"/></w:sectPr>
</w:body>
</w:document>
"@
Set-Content -LiteralPath (Join-Path $TempDir "word\document.xml") -Value $documentXml -Encoding UTF8 -NoNewline

$stylesXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
<w:docDefaults>
<w:rPrDefault><w:rPr><w:rFonts w:ascii="Calibri" w:hAnsi="Calibri" w:cs="Calibri"/><w:sz w:val="22"/><w:lang w:val="vi-VN"/></w:rPr></w:rPrDefault>
</w:docDefaults>
<w:style w:type="paragraph" w:default="1" w:styleId="Normal"><w:name w:val="Normal"/><w:qFormat/><w:pPr><w:spacing w:after="120" w:line="288" w:lineRule="auto"/></w:pPr></w:style>
<w:style w:type="paragraph" w:styleId="Title"><w:name w:val="Title"/><w:basedOn w:val="Normal"/><w:qFormat/><w:pPr><w:spacing w:after="300"/></w:pPr><w:rPr><w:b/><w:sz w:val="40"/><w:color w:val="1F3864"/></w:rPr></w:style>
<w:style w:type="paragraph" w:styleId="Heading1"><w:name w:val="heading 1"/><w:basedOn w:val="Normal"/><w:qFormat/><w:pPr><w:spacing w:before="360" w:after="160"/></w:pPr><w:rPr><w:b/><w:sz w:val="30"/><w:color w:val="2E5395"/></w:rPr></w:style>
<w:style w:type="paragraph" w:styleId="Heading2"><w:name w:val="heading 2"/><w:basedOn w:val="Normal"/><w:qFormat/><w:pPr><w:spacing w:before="240" w:after="120"/></w:pPr><w:rPr><w:b/><w:sz w:val="26"/><w:color w:val="31849B"/></w:rPr></w:style>
<w:style w:type="paragraph" w:styleId="ListParagraph"><w:name w:val="List Paragraph"/><w:basedOn w:val="Normal"/><w:qFormat/><w:pPr><w:ind w:left="360"/><w:spacing w:after="80"/></w:pPr></w:style>
</w:styles>
"@
Set-Content -LiteralPath (Join-Path $TempDir "word\styles.xml") -Value $stylesXml -Encoding UTF8 -NoNewline

$contentTypesXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
<Default Extension="xml" ContentType="application/xml"/>
<Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
<Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
<Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>
<Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>
</Types>
"@
Set-Content -LiteralPath (Join-Path $TempDir "[Content_Types].xml") -Value $contentTypesXml -Encoding UTF8 -NoNewline

$rootRels = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
<Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>
<Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/>
</Relationships>
"@
Set-Content -LiteralPath (Join-Path $TempDir "_rels\.rels") -Value $rootRels -Encoding UTF8 -NoNewline

$docRels = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
</Relationships>
"@
Set-Content -LiteralPath (Join-Path $TempDir "word\_rels\document.xml.rels") -Value $docRels -Encoding UTF8 -NoNewline

$coreXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:dcterms="http://purl.org/dc/terms/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
<dc:title>Meowdoku - Bao cao du an de phong van</dc:title>
<dc:creator>Meowdoku Project</dc:creator>
</cp:coreProperties>
"@
Set-Content -LiteralPath (Join-Path $TempDir "docProps\core.xml") -Value $coreXml -Encoding UTF8 -NoNewline

$appXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties">
<Application>Meowdoku Docx Generator</Application>
</Properties>
"@
Set-Content -LiteralPath (Join-Path $TempDir "docProps\app.xml") -Value $appXml -Encoding UTF8 -NoNewline

if (Test-Path $OutputPath) { Remove-Item $OutputPath -Force }
$zipPath = [System.IO.Path]::ChangeExtension($OutputPath, ".zip")
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
Get-ChildItem -Path $TempDir -Recurse -File | ForEach-Object {
    $relativePath = $_.FullName.Substring($TempDir.Length + 1) -replace "\\", "/"
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $relativePath, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
}
$zip.Dispose()

Rename-Item -LiteralPath $zipPath -NewName (Split-Path $OutputPath -Leaf)
Remove-Item $TempDir -Recurse -Force

Write-Output "DONE: $OutputPath"


