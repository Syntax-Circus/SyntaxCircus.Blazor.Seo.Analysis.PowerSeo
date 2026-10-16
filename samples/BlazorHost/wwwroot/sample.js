window.sampleFixtures = async () => {
    const response = await fetch(new URL('qualification-fixtures', document.baseURI));
    if (!response.ok) throw new Error('Cannot load fixtures');
    return response.text();
};
