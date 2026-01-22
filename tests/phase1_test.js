
async function runTest() {
    const baseUrl = 'http://localhost:5015';
    const tenantId = 'tenant-123';
    const fileName = 'test-doc.pdf';

    console.log('--- Phase 1 Test Started ---');

    // 1. POST to /api/documents/upload
    console.log(`1. Uploading document: ${fileName} for tenant: ${tenantId}...`);
    const uploadRes = await fetch(`${baseUrl}/api/documents/upload?tenantId=${tenantId}&fileName=${fileName}`, {
        method: 'POST'
    });

    if (!uploadRes.ok) {
        console.error('Upload failed:', await uploadRes.text());
        return;
    }

    const { documentId, jobId } = await uploadRes.json();
    console.log(`   Success! DocumentId: ${documentId}, JobId: ${jobId}`);

    // 2. Wait 1s
    console.log('2. Waiting 1s...');
    await new Promise(resolve => setTimeout(resolve, 1000));

    // 3. GET /api/documents/{id} (should be Validating)
    console.log(`3. Checking status (should be Validating)...`);
    const status1Res = await fetch(`${baseUrl}/api/documents/${documentId}`);
    const doc1 = await status1Res.json();
    console.log(`   Current Status: ${doc1.status}`);

    if (doc1.status !== 1) { // 1 is Validating based on DocumentStatus enum usually (Uploaded=0, Validating=1)
        console.warn(`   WARNING: Expected status Validating (1), but got ${doc1.statusText} (${doc1.status})`);
    } else {
        console.log('   MATCH: Status is Validating');
    }

    // 4. Wait 2s
    console.log('4. Waiting 2s...');
    await new Promise(resolve => setTimeout(resolve, 2000));

    // 5. GET /api/documents/{id} (should be OcrPending or ValidationFailed)
    console.log(`5. Checking final status (should be OcrPending or ValidationFailed)...`);
    const status2Res = await fetch(`${baseUrl}/api/documents/${documentId}`);
    const doc2 = await status2Res.json();
    console.log(`   Final Status: ${doc2.status}`);

    if (doc2.status === 2 || doc2.status === 3) { // 2 is ValidationFailed, 3 is OcrPending
        console.log('   MATCH: Status is ' + (doc2.status === 3 ? 'OcrPending' : 'ValidationFailed'));
    } else {
        console.error(`   ERROR: Unexpected final status: ${doc2.status}`);
    }

    console.log('--- Phase 1 Test Completed ---');
}

runTest().catch(console.error);
