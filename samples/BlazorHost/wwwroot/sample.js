window.sampleFixtures = async () => {
    const response = await fetch(new URL('fixtures.json', document.baseURI));
    if (!response.ok) throw new Error('Cannot load fixtures');
    return response.text();
};
