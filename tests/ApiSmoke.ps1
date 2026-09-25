param([string]$BaseUrl = 'http://127.0.0.1:5207')
$ErrorActionPreference = 'Stop'
$createdTournament = $null
$createdTeams = @()
$createdMatch = $null

function Send-Json([string]$Method, [string]$Path, $Body = $null) {
    $arguments = @{ Uri = "$BaseUrl$Path"; Method = $Method; SkipHttpErrorCheck = $true }
    if ($null -ne $Body) {
        $arguments.ContentType = 'application/json'
        $arguments.Body = ConvertTo-Json -InputObject $Body -Depth 8 -Compress
    }
    $response = Invoke-WebRequest @arguments
    $data = if ($response.Content) { $response.Content | ConvertFrom-Json } else { $null }
    return @{ Status = [int]$response.StatusCode; Data = $data }
}

function Assert-Status($Response, [int]$Expected, [string]$Message) {
    if ($Response.Status -ne $Expected) { throw "$Message`: expected $Expected, got $($Response.Status)" }
}

try {
    $list = Send-Json GET '/api/tournaments'
    Assert-Status $list 200 'List tournaments'
    if ($list.Data.Count -lt 2) { throw 'Seed tournaments missing' }

    $page = Send-Json GET '/api/matches?skip=0&limit=1'
    Assert-Status $page 200 'Paginate matches'
    if ($page.Data.items.Count -ne 1 -or $page.Data.total -lt 22 -or -not $page.Data.nextLink.StartsWith("$BaseUrl/api/matches?skip=1")) {
        throw 'Pagination data invalid'
    }
    Assert-Status (Send-Json GET '/api/matches?limit=101') 400 'Reject excessive page limit'

    $slug = "qa-$PID-$([DateTimeOffset]::UtcNow.ToUnixTimeSeconds())"
    $tournament = @{ slug = $slug; name = 'QA Tournament'; season = 2030; city = 'Test'; startsOn = '2030-01-01'; endsOn = '2030-01-31'; isComplete = $false; sourceUrl = 'https://example.org/tournament'; snapshotOn = '2030-01-01' }
    $created = Send-Json POST '/api/tournaments' $tournament
    Assert-Status $created 201 'Create tournament'
    $createdTournament = $created.Data.id

    foreach ($short in @('QAA', 'QAB')) {
        $team = @{ tournamentId = $createdTournament; name = "QA $short"; shortName = $short; region = 'Test'; accentColor = '#76B8BA'; markUrl = '/img/marks/nrg.svg' }
        $createdTeam = Send-Json POST '/api/teams' $team
        Assert-Status $createdTeam 201 "Create team $short"
        $createdTeams += $createdTeam.Data.id
    }

    $match = @{ tournamentId = $createdTournament; code = 'QA1'; lane = 'upper'; round = 1; slot = 1; label = 'QA match'; teamAId = $createdTeams[0]; teamBId = $createdTeams[1]; scoreA = 2; scoreB = 1; bestOf = 3 }
    $invalid = $match.Clone(); $invalid.scoreA = 1; $invalid.scoreB = 1
    Assert-Status (Send-Json POST '/api/matches' $invalid) 400 'Reject tied score'
    $created = Send-Json POST '/api/matches' $match
    Assert-Status $created 201 'Create match'
    $createdMatch = $created.Data.id
    Assert-Status (Send-Json DELETE "/api/teams/$($createdTeams[0])") 409 'Protect referenced team'

    $match.scoreA = 0; $match.scoreB = 2
    Assert-Status (Send-Json PUT "/api/matches/$createdMatch" $match) 204 'Update match'
    $read = Send-Json GET "/api/matches/$createdMatch"
    if ($read.Data.scoreA -ne 0 -or $read.Data.scoreB -ne 2) { throw 'Updated match not persisted' }

    Assert-Status (Send-Json DELETE "/api/matches/$createdMatch") 204 'Delete match'
    $createdMatch = $null
    foreach ($id in $createdTeams) { Assert-Status (Send-Json DELETE "/api/teams/$id") 204 "Delete team $id" }
    $createdTeams = @()
    Assert-Status (Send-Json DELETE "/api/tournaments/$createdTournament") 204 'Delete tournament'
    Assert-Status (Send-Json GET "/api/tournaments/$createdTournament") 404 'Deleted tournament absent'
    $createdTournament = $null
    'API smoke passed: CRUD, validation, conflict, pagination, 404.'
}
finally {
    if ($createdMatch) { Send-Json DELETE "/api/matches/$createdMatch" | Out-Null }
    foreach ($id in $createdTeams) { Send-Json DELETE "/api/teams/$id" | Out-Null }
    if ($createdTournament) { Send-Json DELETE "/api/tournaments/$createdTournament" | Out-Null }
}
